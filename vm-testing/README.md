# Windows 11 VM Manager

Automatically creates and manages Windows 11 virtual machines with shared filesystem access to the current working directory. Supports SPICE remote access for seamless desktop integration.

## Features

- **Automated VM Creation**: One-command Windows 11 VM deployment
- **Shared Filesystem**: Current working directory accessible from Windows VM
- **SPICE Integration**: Remote desktop access with clipboard sharing
- **Unattended Installation**: Optional automated Windows setup
- **VirtIO Drivers**: Automatic download and integration of Windows drivers
- **VM Management**: Start, stop, and destroy VMs programmatically

## Requirements

### System Requirements
- Linux host with KVM/QEMU support
- Hardware virtualization enabled (Intel VT-x or AMD-V)
- Minimum 8GB RAM (4GB+ available for VM)
- 80GB+ free disk space

### Software Dependencies
```bash
sudo apt-get install qemu-kvm libvirt-daemon-system libvirt-clients \
                     virt-manager python3-libvirt virt-viewer virtio-win \
                     genisoimage
```

### User Permissions
```bash
sudo usermod -aG libvirt $USER
sudo usermod -aG kvm $USER
# Log out and back in for group changes to take effect
```

### Enable Services
```bash
sudo systemctl enable libvirtd
sudo systemctl start libvirtd
```

## Quick Start

### 1. Download Windows 11 ISO
Visit [Microsoft Evaluation Center](https://www.microsoft.com/en-us/evalcenter/download-windows-11-enterprise) and download Windows 11 Enterprise (90-day evaluation).

### 2. Create VM
```bash
cd /path/to/your/project
python3 vm-testing/windows_vm_manager.py --iso /path/to/windows11.iso
```

### 3. Connect
The script will automatically:
- Create a new VM with 8GB RAM and 80GB disk
- Mount your current directory as a shared folder
- Launch SPICE client for remote access
- Provide connection details

## Usage Examples

### Basic VM Creation
```bash
# Create VM with current directory shared
python3 windows_vm_manager.py --iso windows11.iso

# Create VM with custom name and directory
python3 windows_vm_manager.py --name dev-vm --cwd /home/user/projects --iso windows11.iso

# Create VM without launching SPICE client
python3 windows_vm_manager.py --iso windows11.iso --no-client

# Create VM without unattended installation
python3 windows_vm_manager.py --iso windows11.iso --no-unattended
```

### VM Management
```bash
# List all VMs
python3 windows_vm_manager.py --list

# Stop a VM gracefully
python3 windows_vm_manager.py --stop vm-name

# Force stop and remove VM
python3 windows_vm_manager.py --destroy vm-name
```

### Manual Connection
```bash
# Connect to running VM
remote-viewer spice://localhost:PORT

# Find SPICE port
virsh dumpxml vm-name | grep "graphics type='spice'"
```

## Windows Setup Process

### 1. Initial Installation
- VM boots from Windows 11 ISO
- If unattended installation is enabled, Windows installs automatically
- Default user: `vmuser` (no password)

### 2. Install VirtIO Drivers
1. Open Device Manager in Windows
2. Install drivers from drive D: (virtio-win CD)
3. Install all VirtIO devices for best performance

### 3. Access Shared Folder
The shared folder will be available through various methods:
- **9P Protocol**: Install 9P client in Windows
- **Network Share**: Set up SMB/CIFS sharing
- **SSH/SFTP**: Access via network protocols

## Configuration Options

### VM Specifications
- **Memory**: 8GB (configurable in script)
- **vCPUs**: 4 cores
- **Disk**: 80GB QCOW2 (thin provisioned)
- **Graphics**: QXL with SPICE
- **Network**: User-mode networking (NAT)

### SPICE Features
- Clipboard sharing between host and guest
- USB device redirection
- Audio passthrough
- Dynamic resolution adjustment

## Troubleshooting

### Common Issues

#### 1. Permission Denied
```bash
# Check user groups
groups $USER
# Should include 'libvirt' and 'kvm'

# Fix permissions
sudo usermod -aG libvirt,kvm $USER
# Log out and back in
```

#### 2. KVM Not Available
```bash
# Check CPU virtualization support
grep -E '(vmx|svm)' /proc/cpuinfo

# Load KVM module
sudo modprobe kvm-intel  # or kvm-amd
```

#### 3. SPICE Connection Failed
```bash
# Check if VM is running
virsh list --all

# Check SPICE port
virsh dumpxml vm-name | grep spice

# Test connection
remote-viewer spice://localhost:PORT
```

#### 4. Shared Folder Not Working
- Install VirtIO drivers in Windows
- Check 9P filesystem support
- Alternative: Use network sharing (SMB/SSH)

### Debug Mode
```bash
# Enable libvirt debug logging
export LIBVIRT_DEBUG=1

# Check QEMU logs
journalctl -u libvirtd -f
```

## Security Considerations

### Network Isolation
- VMs use user-mode networking (NAT) by default
- No direct network access to host services
- Outbound internet access available

### Shared Filesystem
- 9P uses mapped security model
- Files created in VM owned by host user
- Consider read-only sharing for sensitive data

### Evaluation License
- Windows 11 Enterprise evaluation expires in 90 days
- VM will shut down hourly after expiration
- Not suitable for production use

## Advanced Usage

### Custom VM Configuration
Edit the `generate_vm_xml()` method to customize:
- Memory and CPU allocation
- Additional storage devices
- Network configuration
- Hardware acceleration options

### Automated Deployment
```python
from windows_vm_manager import WindowsVMManager

# Programmatic VM creation
manager = WindowsVMManager(cwd_path="/project/path")
domain = manager.create_and_start_vm(iso_path, virtio_path)
```

### Integration with CI/CD
```bash
# Headless VM for testing
python3 windows_vm_manager.py --iso windows11.iso --no-client

# Run tests and cleanup
python3 run_tests.py
python3 windows_vm_manager.py --destroy test-vm
```

## File Structure

```
vm-testing/
├── windows_vm_manager.py    # Main VM management script
├── README.md               # This documentation
└── examples/              # Example configurations
    ├── custom_vm.py       # Custom VM configuration
    └── batch_deployment.py # Multiple VM deployment
```

## Contributing

Feel free to enhance the script with additional features:
- GUI interface
- Different Windows versions support
- Cloud provider integration
- Snapshot management
- Performance monitoring

## License

This script is provided for educational and development purposes. Windows 11 licensing is subject to Microsoft's terms and conditions.