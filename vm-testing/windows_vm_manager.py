#!/usr/bin/env python3
"""
Windows 11 VM Manager
Automatically creates and manages Windows 11 VMs with shared filesystem access.
Supports SPICE and RDP remote access protocols.
"""

import libvirt
import subprocess
import os
import time
import xml.etree.ElementTree as ET
from pathlib import Path
import sys
import urllib.request
import json
import hashlib


class WindowsVMManager:
    def __init__(self, cwd_path=None, vm_name=None):
        self.cwd_path = Path(cwd_path or os.getcwd()).resolve()
        self.vm_name = vm_name or f"windows11-{int(time.time())}"
        self.conn = None
        self.vm_dir = Path("/tmp/vm-storage")
        self.vm_dir.mkdir(exist_ok=True)
        
    def check_dependencies(self):
        """Check if required dependencies are installed"""
        required_commands = [
            "qemu-img", "qemu-system-x86_64", "virsh", "remote-viewer"
        ]
        
        missing = []
        for cmd in required_commands:
            if subprocess.run(["which", cmd], capture_output=True).returncode != 0:
                missing.append(cmd)
        
        if missing:
            print("Missing required dependencies:")
            for cmd in missing:
                print(f"  - {cmd}")
            print("\nInstall with:")
            print("sudo apt-get install qemu-kvm libvirt-daemon-system libvirt-clients \\")
            print("                     virt-manager python3-libvirt virt-viewer virtio-win")
            return False
        return True
    
    def check_virtualization_support(self):
        """Check if hardware virtualization is supported"""
        try:
            with open("/proc/cpuinfo", "r") as f:
                cpuinfo = f.read()
                if "vmx" not in cpuinfo and "svm" not in cpuinfo:
                    print("Hardware virtualization not supported or not enabled in BIOS")
                    return False
        except FileNotFoundError:
            print("Cannot check CPU virtualization support")
            return False
        
        # Check if KVM module is loaded
        if subprocess.run(["lsmod"], capture_output=True, text=True).stdout.find("kvm") == -1:
            print("KVM module not loaded. Try: sudo modprobe kvm-intel (or kvm-amd)")
            return False
            
        return True
    
    def download_windows_iso(self, download_dir=None):
        """
        Guide user through Windows 11 ISO download process
        Returns path to downloaded ISO or None if not found
        """
        download_dir = Path(download_dir or self.vm_dir)
        iso_patterns = [
            "Win11_*_English_x64v*.iso",
            "win11*.iso",
            "Windows11*.iso"
        ]
        
        # Check if ISO already exists
        for pattern in iso_patterns:
            existing_isos = list(download_dir.glob(pattern))
            if existing_isos:
                print(f"Found existing Windows 11 ISO: {existing_isos[0]}")
                return existing_isos[0]
        
        print("Windows 11 ISO not found. Please download it manually:")
        print("1. Visit: https://www.microsoft.com/en-us/evalcenter/download-windows-11-enterprise")
        print("2. Select 'Windows 11 Enterprise (Evaluation)'")
        print("3. Choose your language and download the 64-bit ISO")
        print(f"4. Save it to: {download_dir}")
        print("5. Re-run this script")
        
        return None
    
    def download_virtio_drivers(self):
        """Download VirtIO drivers for Windows"""
        virtio_iso_path = self.vm_dir / "virtio-win.iso"
        
        if virtio_iso_path.exists():
            print(f"VirtIO drivers already exist at: {virtio_iso_path}")
            return virtio_iso_path
        
        # Check system location first
        system_paths = [
            "/usr/share/virtio-win/virtio-win.iso",
            "/usr/share/virtio-win/virtio-win-*.iso"
        ]
        
        for path_pattern in system_paths:
            system_files = list(Path("/").glob(path_pattern.lstrip("/")))
            if system_files:
                print(f"Using system VirtIO drivers: {system_files[0]}")
                return system_files[0]
        
        print("Downloading VirtIO drivers...")
        virtio_url = "https://fedorapeople.org/groups/virt/virtio-win/direct-downloads/stable-virtio/virtio-win.iso"
        
        try:
            urllib.request.urlretrieve(virtio_url, virtio_iso_path)
            print(f"Downloaded VirtIO drivers to: {virtio_iso_path}")
            return virtio_iso_path
        except Exception as e:
            print(f"Failed to download VirtIO drivers: {e}")
            print("Please install virtio-win package: sudo apt-get install virtio-win")
            return None

    def create_vm_disk(self, size_gb=80):
        """Create VM disk image"""
        disk_path = self.vm_dir / f"{self.vm_name}.qcow2"
        
        if disk_path.exists():
            print(f"Disk already exists: {disk_path}")
            return disk_path
            
        print(f"Creating {size_gb}GB disk image...")
        subprocess.run([
            "qemu-img", "create", "-f", "qcow2", str(disk_path), f"{size_gb}G"
        ], check=True)
        
        print(f"Created disk: {disk_path}")
        return disk_path
    
    def create_unattended_xml(self):
        """Create unattended installation XML for Windows 11"""
        autounattend_content = """<?xml version="1.0" encoding="utf-8"?>
<unattend xmlns="urn:schemas-microsoft-com:unattend">
    <settings pass="windowsPE">
        <component name="Microsoft-Windows-Setup" processorArchitecture="amd64" publicKeyToken="31bf3856ad364e35" language="neutral" versionScope="nonSxS" xmlns:wcm="http://schemas.microsoft.com/WMIConfig/2002/State" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
            <DiskConfiguration>
                <Disk wcm:action="add">
                    <CreatePartitions>
                        <CreatePartition wcm:action="add">
                            <Order>1</Order>
                            <Size>100</Size>
                            <Type>Primary</Type>
                        </CreatePartition>
                        <CreatePartition wcm:action="add">
                            <Order>2</Order>
                            <Extend>true</Extend>
                            <Type>Primary</Type>
                        </CreatePartition>
                    </CreatePartitions>
                    <ModifyPartitions>
                        <ModifyPartition wcm:action="add">
                            <Active>true</Active>
                            <Format>NTFS</Format>
                            <Label>System</Label>
                            <Order>1</Order>
                            <PartitionID>1</PartitionID>
                        </ModifyPartition>
                        <ModifyPartition wcm:action="add">
                            <Format>NTFS</Format>
                            <Label>Windows</Label>
                            <Letter>C</Letter>
                            <Order>2</Order>
                            <PartitionID>2</PartitionID>
                        </ModifyPartition>
                    </ModifyPartitions>
                    <DiskID>0</DiskID>
                    <WillWipeDisk>true</WillWipeDisk>
                </Disk>
            </DiskConfiguration>
            <ImageInstall>
                <OSImage>
                    <InstallTo>
                        <DiskID>0</DiskID>
                        <PartitionID>2</PartitionID>
                    </InstallTo>
                </OSImage>
            </ImageInstall>
            <UserData>
                <AcceptEula>true</AcceptEula>
                <FullName>VM User</FullName>
                <Organization>Development</Organization>
            </UserData>
        </component>
        <component name="Microsoft-Windows-International-Core-WinPE" processorArchitecture="amd64" publicKeyToken="31bf3856ad364e35" language="neutral" versionScope="nonSxS" xmlns:wcm="http://schemas.microsoft.com/WMIConfig/2002/State" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
            <SetupUILanguage>
                <UILanguage>en-US</UILanguage>
            </SetupUILanguage>
            <InputLocale>en-US</InputLocale>
            <SystemLocale>en-US</SystemLocale>
            <UILanguage>en-US</UILanguage>
            <UserLocale>en-US</UserLocale>
        </component>
    </settings>
    <settings pass="oobeSystem">
        <component name="Microsoft-Windows-Shell-Setup" processorArchitecture="amd64" publicKeyToken="31bf3856ad364e35" language="neutral" versionScope="nonSxS" xmlns:wcm="http://schemas.microsoft.com/WMIConfig/2002/State" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
            <OOBE>
                <HideEULAPage>true</HideEULAPage>
                <HideLocalAccountScreen>true</HideLocalAccountScreen>
                <HideOEMRegistrationScreen>true</HideOEMRegistrationScreen>
                <HideOnlineAccountScreens>true</HideOnlineAccountScreens>
                <HideWirelessSetupInOOBE>true</HideWirelessSetupInOOBE>
                <NetworkLocation>Work</NetworkLocation>
                <ProtectYourPC>1</ProtectYourPC>
            </OOBE>
            <UserAccounts>
                <LocalAccounts>
                    <LocalAccount wcm:action="add">
                        <Password>
                            <Value></Value>
                            <PlainText>true</PlainText>
                        </Password>
                        <Description>VM User Account</Description>
                        <DisplayName>VM User</DisplayName>
                        <Group>Administrators</Group>
                        <Name>vmuser</Name>
                    </LocalAccount>
                </LocalAccounts>
            </UserAccounts>
            <AutoLogon>
                <Password>
                    <Value></Value>
                    <PlainText>true</PlainText>
                </Password>
                <Enabled>true</Enabled>
                <LogonCount>1</LogonCount>
                <Username>vmuser</Username>
            </AutoLogon>
        </component>
    </settings>
</unattend>"""
        
        autounattend_path = self.vm_dir / "autounattend.xml"
        with open(autounattend_path, 'w') as f:
            f.write(autounattend_content)
        
        # Create ISO with autounattend.xml
        autounattend_iso = self.vm_dir / "autounattend.iso"
        subprocess.run([
            "genisoimage", "-o", str(autounattend_iso), 
            "-J", "-r", str(autounattend_path)
        ], check=True, capture_output=True)
        
        return autounattend_iso

    def generate_vm_xml(self, disk_path, iso_path, virtio_path, autounattend_iso=None):
        """Generate libvirt XML configuration"""
        memory_mb = 8192
        vcpus = 4
        
        # Generate random MAC address
        import random
        mac = "52:54:00:" + ":".join([f"{random.randint(0, 255):02x}" for _ in range(3)])
        
        xml = f"""<domain type='kvm'>
  <name>{self.vm_name}</name>
  <memory unit='MiB'>{memory_mb}</memory>
  <currentMemory unit='MiB'>{memory_mb}</currentMemory>
  <vcpu placement='static'>{vcpus}</vcpu>
  <os>
    <type arch='x86_64' machine='pc-q35-6.2'>hvm</type>
    <loader readonly='yes' type='pflash'>/usr/share/OVMF/OVMF_CODE.fd</loader>
    <nvram>/var/lib/libvirt/qemu/nvram/{self.vm_name}_VARS.fd</nvram>
    <boot dev='cdrom'/>
    <boot dev='hd'/>
  </os>
  <features>
    <acpi/>
    <apic/>
    <hyperv mode='custom'>
      <relaxed state='on'/>
      <vapic state='on'/>
      <spinlocks state='on' retries='8191'/>
      <vpindex state='on'/>
      <synic state='on'/>
      <stimer state='on'/>
      <reset state='on'/>
      <vendor_id state='on' value='KVM Hv'/>
      <frequencies state='on'/>
    </hyperv>
    <kvm>
      <hidden state='on'/>
    </kvm>
    <vmport state='off'/>
  </features>
  <cpu mode='host-model' check='partial'>
    <topology sockets='1' dies='1' cores='{vcpus}' threads='1'/>
  </cpu>
  <clock offset='localtime'>
    <timer name='rtc' tickpolicy='catchup'/>
    <timer name='pit' tickpolicy='delay'/>
    <timer name='hpet' present='no'/>
    <timer name='hypervclock' present='yes'/>
  </clock>
  <on_poweroff>destroy</on_poweroff>
  <on_reboot>restart</on_reboot>
  <on_crash>destroy</on_crash>
  <pm>
    <suspend-to-mem enabled='no'/>
    <suspend-to-disk enabled='no'/>
  </pm>
  <devices>
    <emulator>/usr/bin/qemu-system-x86_64</emulator>
    <disk type='file' device='disk'>
      <driver name='qemu' type='qcow2' cache='writeback'/>
      <source file='{disk_path}'/>
      <target dev='vda' bus='virtio'/>
      <address type='pci' domain='0x0000' bus='0x03' slot='0x00' function='0x0'/>
    </disk>
    <disk type='file' device='cdrom'>
      <driver name='qemu' type='raw'/>
      <source file='{iso_path}'/>
      <target dev='sda' bus='sata'/>
      <readonly/>
      <address type='drive' controller='0' bus='0' target='0' unit='0'/>
    </disk>
    <disk type='file' device='cdrom'>
      <driver name='qemu' type='raw'/>
      <source file='{virtio_path}'/>
      <target dev='sdb' bus='sata'/>
      <readonly/>
      <address type='drive' controller='0' bus='0' target='0' unit='1'/>
    </disk>"""

        if autounattend_iso:
            xml += f"""
    <disk type='file' device='cdrom'>
      <driver name='qemu' type='raw'/>
      <source file='{autounattend_iso}'/>
      <target dev='sdc' bus='sata'/>
      <readonly/>
      <address type='drive' controller='0' bus='0' target='0' unit='2'/>
    </disk>"""

        xml += f"""
    <controller type='usb' index='0' model='qemu-xhci' ports='15'>
      <address type='pci' domain='0x0000' bus='0x02' slot='0x00' function='0x0'/>
    </controller>
    <controller type='sata' index='0'>
      <address type='pci' domain='0x0000' bus='0x00' slot='0x1f' function='0x2'/>
    </controller>
    <controller type='pci' index='0' model='pcie-root'/>
    <controller type='pci' index='1' model='pcie-root-port'>
      <model name='pcie-root-port'/>
      <target chassis='1' port='0x10'/>
      <address type='pci' domain='0x0000' bus='0x00' slot='0x02' function='0x0' multifunction='on'/>
    </controller>
    <controller type='pci' index='2' model='pcie-root-port'>
      <model name='pcie-root-port'/>
      <target chassis='2' port='0x11'/>
      <address type='pci' domain='0x0000' bus='0x00' slot='0x02' function='0x1'/>
    </controller>
    <controller type='pci' index='3' model='pcie-root-port'>
      <model name='pcie-root-port'/>
      <target chassis='3' port='0x12'/>
      <address type='pci' domain='0x0000' bus='0x00' slot='0x02' function='0x2'/>
    </controller>
    <filesystem type='mount' accessmode='mapped'>
      <source dir='{self.cwd_path}'/>
      <target dir='hostshare'/>
      <address type='pci' domain='0x0000' bus='0x01' slot='0x00' function='0x0'/>
    </filesystem>
    <interface type='user'>
      <mac address='{mac}'/>
      <model type='virtio'/>
      <address type='pci' domain='0x0000' bus='0x01' slot='0x01' function='0x0'/>
    </interface>
    <serial type='pty'>
      <target type='isa-serial' port='0'>
        <model name='isa-serial'/>
      </target>
    </serial>
    <console type='pty'>
      <target type='serial' port='0'/>
    </console>
    <channel type='spicevmc'>
      <target type='virtio' name='com.redhat.spice.0'/>
      <address type='virtio-serial' controller='0' bus='0' port='1'/>
    </channel>
    <input type='tablet' bus='usb'>
      <address type='usb' bus='0' port='1'/>
    </input>
    <input type='mouse' bus='ps2'/>
    <input type='keyboard' bus='ps2'/>
    <graphics type='spice' autoport='yes'>
      <listen type='address'/>
      <image compression='off'/>
    </graphics>
    <sound model='ich9'>
      <address type='pci' domain='0x0000' bus='0x00' slot='0x1b' function='0x0'/>
    </sound>
    <audio id='1' type='spice'/>
    <video>
      <model type='qxl' ram='65536' vram='65536' vgamem='16384' heads='1' primary='yes'/>
      <address type='pci' domain='0x0000' bus='0x00' slot='0x01' function='0x0'/>
    </video>
    <redirdev bus='usb' type='spicevmc'>
      <address type='usb' bus='0' port='2'/>
    </redirdev>
    <redirdev bus='usb' type='spicevmc'>
      <address type='usb' bus='0' port='3'/>
    </redirdev>
    <memballoon model='virtio'>
      <address type='pci' domain='0x0000' bus='0x04' slot='0x00' function='0x0'/>
    </memballoon>
    <rng model='virtio'>
      <backend model='random'>/dev/urandom</backend>
      <address type='pci' domain='0x0000' bus='0x05' slot='0x00' function='0x0'/>
    </rng>
  </devices>
</domain>"""
        return xml

    def create_and_start_vm(self, iso_path, virtio_path, create_unattended=True):
        """Create and start the VM"""
        try:
            self.conn = libvirt.open('qemu:///system')
        except libvirt.libvirtError as e:
            print(f"Failed to connect to libvirt: {e}")
            print("Make sure libvirtd is running and you're in the libvirt group")
            return None
        
        # Create disk
        disk_path = self.create_vm_disk()
        
        # Create unattended installation if requested
        autounattend_iso = None
        if create_unattended:
            try:
                autounattend_iso = self.create_unattended_xml()
                print(f"Created unattended installation ISO: {autounattend_iso}")
            except subprocess.CalledProcessError:
                print("Failed to create unattended installation (genisoimage not found)")
                print("Install with: sudo apt-get install genisoimage")
        
        # Generate XML
        xml = self.generate_vm_xml(disk_path, iso_path, virtio_path, autounattend_iso)
        
        try:
            # Check if VM already exists
            try:
                existing_domain = self.conn.lookupByName(self.vm_name)
                print(f"VM '{self.vm_name}' already exists")
                if existing_domain.isActive():
                    print("VM is already running")
                    return existing_domain
                else:
                    print("Starting existing VM...")
                    existing_domain.create()
                    return existing_domain
            except libvirt.libvirtError:
                pass  # VM doesn't exist, create it
            
            # Create and start domain
            domain = self.conn.defineXML(xml)
            domain.create()
            
            print(f"VM '{self.vm_name}' created and started successfully")
            return domain
            
        except libvirt.libvirtError as e:
            print(f"Failed to create VM: {e}")
            return None

    def get_vm_info(self, domain):
        """Get VM connection information"""
        try:
            xml_desc = domain.XMLDesc()
            root = ET.fromstring(xml_desc)
            
            # Get SPICE port
            graphics = root.find('.//graphics[@type="spice"]')
            spice_port = graphics.get('port') if graphics is not None else None
            
            # Get VM state
            state = domain.state()[0]
            state_names = {
                libvirt.VIR_DOMAIN_NOSTATE: 'No state',
                libvirt.VIR_DOMAIN_RUNNING: 'Running',
                libvirt.VIR_DOMAIN_BLOCKED: 'Blocked',
                libvirt.VIR_DOMAIN_PAUSED: 'Paused',
                libvirt.VIR_DOMAIN_SHUTDOWN: 'Shutting down',
                libvirt.VIR_DOMAIN_SHUTOFF: 'Shut off',
                libvirt.VIR_DOMAIN_CRASHED: 'Crashed'
            }
            
            return {
                'name': domain.name(),
                'state': state_names.get(state, 'Unknown'),
                'spice_port': spice_port,
                'shared_folder': str(self.cwd_path)
            }
            
        except Exception as e:
            print(f"Failed to get VM info: {e}")
            return None

    def launch_spice_client(self, port):
        """Launch SPICE client to connect to VM"""
        try:
            subprocess.Popen([
                "remote-viewer", f"spice://localhost:{port}"
            ], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            print(f"Launched SPICE client connecting to port {port}")
        except FileNotFoundError:
            print("remote-viewer not found. Install with: sudo apt-get install virt-viewer")
        except Exception as e:
            print(f"Failed to launch SPICE client: {e}")

    def stop_vm(self):
        """Stop the VM"""
        if not self.conn:
            return
            
        try:
            domain = self.conn.lookupByName(self.vm_name)
            if domain.isActive():
                domain.shutdown()
                print(f"Shutting down VM '{self.vm_name}'...")
            else:
                print(f"VM '{self.vm_name}' is not running")
        except libvirt.libvirtError as e:
            print(f"Failed to stop VM: {e}")

    def destroy_vm(self):
        """Forcefully stop and remove the VM"""
        if not self.conn:
            return
            
        try:
            domain = self.conn.lookupByName(self.vm_name)
            if domain.isActive():
                domain.destroy()
            domain.undefine()
            print(f"VM '{self.vm_name}' destroyed")
            
            # Optionally remove disk
            disk_path = self.vm_dir / f"{self.vm_name}.qcow2"
            if disk_path.exists():
                response = input(f"Remove disk file {disk_path}? (y/N): ")
                if response.lower() == 'y':
                    disk_path.unlink()
                    print("Disk file removed")
                    
        except libvirt.libvirtError as e:
            print(f"Failed to destroy VM: {e}")

    def print_connection_info(self, domain):
        """Print connection and usage information"""
        info = self.get_vm_info(domain)
        if not info:
            return
            
        print("\n" + "="*60)
        print("VM CONNECTION INFORMATION")
        print("="*60)
        print(f"VM Name: {info['name']}")
        print(f"State: {info['state']}")
        print(f"SPICE Port: {info['spice_port']}")
        print(f"Shared Folder: {info['shared_folder']}")
        print("\nTo connect manually:")
        print(f"  remote-viewer spice://localhost:{info['spice_port']}")
        print("\nWindows Setup Notes:")
        print("  1. Install Windows normally")
        print("  2. Install VirtIO drivers from drive D: (virtio-win)")
        print("  3. Shared folder will be available after installing 9P drivers")
        print("  4. Default user: vmuser (no password)")
        print("\nVM Management:")
        print(f"  Stop VM: python3 {sys.argv[0]} --stop {self.vm_name}")
        print(f"  Destroy VM: python3 {sys.argv[0]} --destroy {self.vm_name}")
        print("="*60)


def main():
    import argparse
    
    parser = argparse.ArgumentParser(description="Windows 11 VM Manager")
    parser.add_argument("--name", help="VM name (default: auto-generated)")
    parser.add_argument("--cwd", help="Directory to share (default: current directory)")
    parser.add_argument("--iso", help="Path to Windows 11 ISO file")
    parser.add_argument("--stop", help="Stop specified VM")
    parser.add_argument("--destroy", help="Destroy specified VM")
    parser.add_argument("--list", action="store_true", help="List VMs")
    parser.add_argument("--no-unattended", action="store_true", help="Skip unattended installation")
    parser.add_argument("--no-client", action="store_true", help="Don't launch SPICE client")
    
    args = parser.parse_args()
    
    # Handle VM management commands
    if args.stop:
        manager = WindowsVMManager(vm_name=args.stop)
        manager.stop_vm()
        return
        
    if args.destroy:
        manager = WindowsVMManager(vm_name=args.destroy)
        manager.destroy_vm()
        return
        
    if args.list:
        try:
            conn = libvirt.open('qemu:///system')
            domains = conn.listAllDomains()
            print("Virtual Machines:")
            for domain in domains:
                state = "Running" if domain.isActive() else "Stopped"
                print(f"  {domain.name()} - {state}")
        except libvirt.libvirtError as e:
            print(f"Failed to list VMs: {e}")
        return
    
    # Create new VM
    manager = WindowsVMManager(cwd_path=args.cwd, vm_name=args.name)
    
    print("Windows 11 VM Manager")
    print("====================")
    
    # Check prerequisites
    if not manager.check_dependencies():
        return 1
        
    if not manager.check_virtualization_support():
        return 1
    
    # Get Windows ISO
    iso_path = args.iso
    if not iso_path:
        iso_path = manager.download_windows_iso()
        if not iso_path:
            return 1
    
    iso_path = Path(iso_path)
    if not iso_path.exists():
        print(f"Windows ISO not found: {iso_path}")
        return 1
    
    # Get VirtIO drivers
    virtio_path = manager.download_virtio_drivers()
    if not virtio_path:
        return 1
    
    print(f"Using Windows ISO: {iso_path}")
    print(f"Using VirtIO drivers: {virtio_path}")
    print(f"Sharing directory: {manager.cwd_path}")
    
    # Create and start VM
    domain = manager.create_and_start_vm(
        iso_path, 
        virtio_path, 
        create_unattended=not args.no_unattended
    )
    
    if not domain:
        return 1
    
    # Print connection info
    manager.print_connection_info(domain)
    
    # Launch SPICE client
    if not args.no_client:
        info = manager.get_vm_info(domain)
        if info and info['spice_port']:
            time.sleep(2)  # Wait for SPICE to be ready
            manager.launch_spice_client(info['spice_port'])
    
    return 0


if __name__ == "__main__":
    sys.exit(main())