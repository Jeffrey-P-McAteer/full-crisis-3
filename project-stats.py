#!/usr/bin/env python3
"""
Project Statistics Generator

Analyzes git history to generate project statistics and charts.
Currently generates lines of code over time, with support for future metrics.

Usage:
    python project-stats.py <output_directory>

Example:
    python project-stats.py ./stats-output
"""

# /// script
# requires-python = ">=3.8"
# dependencies = [
#     "matplotlib>=3.7.0",
#     "pandas>=2.0.0", 
#     "GitPython>=3.1.0",
#     "seaborn>=0.12.0",
#     "numpy>=1.24.0"
# ]
# ///

import argparse
import os
import sys
import subprocess
from datetime import datetime, timezone
from pathlib import Path
from typing import Dict, List, Tuple, Optional
import tempfile
import shutil

# Third-party imports
try:
    import git
    import pandas as pd
    import matplotlib.pyplot as plt
    import matplotlib.dates as mdates
    import seaborn as sns
    import numpy as np
except ImportError as e:
    print(f"Missing required dependency: {e}")
    print("Please install dependencies using: uv run project-stats.py <output_dir>")
    sys.exit(1)

# Set up matplotlib for headless operation
plt.switch_backend('Agg')
sns.set_style("whitegrid")
plt.style.use('seaborn-v0_8')

class ProjectStatsGenerator:
    """Generates project statistics and charts from git history."""
    
    def __init__(self, repo_path: str = "."):
        """Initialize with repository path."""
        self.repo_path = Path(repo_path).resolve()
        self.repo = git.Repo(repo_path)
        
        # File extension mappings
        self.language_extensions = {
            'python': ['.py'],
            'csharp': ['.cs'],
            'xaml': ['.axaml', '.xaml'],
            'config': ['.csproj', '.sln', '.json', '.xml', '.toml'],
            'docs': ['.md', '.txt', '.rst'],
            'other': []  # Will be populated with remaining extensions
        }
        
    def count_lines_in_file(self, file_content: str) -> int:
        """Count non-empty lines in file content."""
        if not file_content:
            return 0
        lines = file_content.strip().split('\n')
        return len([line for line in lines if line.strip()])
    
    def get_file_language(self, file_path: str) -> str:
        """Determine the language/type of a file based on its extension."""
        ext = Path(file_path).suffix.lower()
        
        for language, extensions in self.language_extensions.items():
            if ext in extensions:
                return language
        
        return 'other'
    
    def analyze_commit(self, commit: git.Commit) -> Dict[str, int]:
        """Analyze a single commit and return line counts by language."""
        stats = {lang: 0 for lang in self.language_extensions.keys()}
        
        try:
            # Get all files in this commit
            for item in commit.tree.traverse():
                if item.type == 'blob':  # It's a file, not a directory
                    try:
                        file_content = item.data_stream.read().decode('utf-8', errors='ignore')
                        line_count = self.count_lines_in_file(file_content)
                        language = self.get_file_language(item.path)
                        stats[language] += line_count
                    except (UnicodeDecodeError, git.exc.GitCommandError):
                        # Skip binary files or files that can't be decoded
                        continue
        except Exception as e:
            print(f"Warning: Could not analyze commit {commit.hexsha[:8]}: {e}")
        
        return stats
    
    def get_commit_history(self, max_commits: int = 100) -> List[Tuple[datetime, Dict[str, int]]]:
        """Get commit history with line count statistics."""
        print(f"Analyzing git history (last {max_commits} commits)...")
        
        commits_data = []
        commits = list(self.repo.iter_commits('HEAD', max_count=max_commits))
        
        # Reverse to get chronological order
        commits.reverse()
        
        for i, commit in enumerate(commits):
            if i % 10 == 0:
                print(f"Processing commit {i+1}/{len(commits)}: {commit.hexsha[:8]}")
            
            commit_date = datetime.fromtimestamp(commit.committed_date, tz=timezone.utc)
            stats = self.analyze_commit(commit)
            commits_data.append((commit_date, stats))
        
        return commits_data
    
    def generate_loc_over_time_chart(self, output_dir: Path):
        """Generate lines of code over time chart."""
        print("Generating lines of code over time chart...")
        
        # Get commit history
        history = self.get_commit_history()
        
        if not history:
            print("No commit history found.")
            return
        
        # Convert to DataFrame
        dates = []
        python_lines = []
        csharp_lines = []
        xaml_lines = []
        total_lines = []
        
        for date, stats in history:
            dates.append(date)
            python_lines.append(stats['python'])
            csharp_lines.append(stats['csharp'])
            xaml_lines.append(stats['xaml'])
            total_lines.append(sum(stats.values()))
        
        df = pd.DataFrame({
            'date': dates,
            'python': python_lines,
            'csharp': csharp_lines,
            'xaml': xaml_lines,
            'total': total_lines
        })
        
        # Create the plot
        fig, (ax1, ax2) = plt.subplots(2, 1, figsize=(12, 10))
        
        # Main languages chart
        ax1.plot(df['date'], df['python'], marker='o', linewidth=2, label='Python', color='#3776ab')
        ax1.plot(df['date'], df['csharp'], marker='s', linewidth=2, label='C#', color='#239120')
        ax1.plot(df['date'], df['xaml'], marker='^', linewidth=2, label='XAML', color='#0078d4')
        
        ax1.set_title('Lines of Code Over Time (by Language)', fontsize=16, fontweight='bold')
        ax1.set_xlabel('Date', fontsize=12)
        ax1.set_ylabel('Lines of Code', fontsize=12)
        ax1.legend()
        ax1.grid(True, alpha=0.3)
        
        # Format x-axis dates
        ax1.xaxis.set_major_formatter(mdates.DateFormatter('%Y-%m-%d'))
        ax1.xaxis.set_major_locator(mdates.DayLocator(interval=max(1, len(dates)//10)))
        plt.setp(ax1.xaxis.get_majorticklabels(), rotation=45)
        
        # Total lines chart
        ax2.plot(df['date'], df['total'], marker='o', linewidth=3, label='Total Lines', color='#d62728')
        ax2.fill_between(df['date'], df['total'], alpha=0.3, color='#d62728')
        
        ax2.set_title('Total Lines of Code Over Time', fontsize=16, fontweight='bold')
        ax2.set_xlabel('Date', fontsize=12)
        ax2.set_ylabel('Total Lines of Code', fontsize=12)
        ax2.legend()
        ax2.grid(True, alpha=0.3)
        
        # Format x-axis dates
        ax2.xaxis.set_major_formatter(mdates.DateFormatter('%Y-%m-%d'))
        ax2.xaxis.set_major_locator(mdates.DayLocator(interval=max(1, len(dates)//10)))
        plt.setp(ax2.xaxis.get_majorticklabels(), rotation=45)
        
        plt.tight_layout()
        
        # Save the chart
        output_file = output_dir / 'lines_of_code_over_time.png'
        plt.savefig(output_file, dpi=300, bbox_inches='tight')
        plt.close()
        
        # Save data as CSV
        csv_file = output_dir / 'lines_of_code_over_time.csv'
        df.to_csv(csv_file, index=False)
        
        print(f"Chart saved to: {output_file}")
        print(f"Data saved to: {csv_file}")
        
        # Print summary statistics
        print("\nSummary Statistics:")
        print(f"Total commits analyzed: {len(history)}")
        print(f"Date range: {df['date'].min().strftime('%Y-%m-%d')} to {df['date'].max().strftime('%Y-%m-%d')}")
        print(f"Final line counts:")
        print(f"  Python: {df['python'].iloc[-1]:,} lines")
        print(f"  C#: {df['csharp'].iloc[-1]:,} lines")
        print(f"  XAML: {df['xaml'].iloc[-1]:,} lines")
        print(f"  Total: {df['total'].iloc[-1]:,} lines")
    
    def generate_language_distribution_chart(self, output_dir: Path):
        """Generate current language distribution pie chart."""
        print("Generating language distribution chart...")
        
        # Get current commit stats
        current_commit = self.repo.head.commit
        stats = self.analyze_commit(current_commit)
        
        # Filter out zero values and 'other'
        filtered_stats = {lang: count for lang, count in stats.items() 
                         if count > 0 and lang != 'other'}
        
        if not filtered_stats:
            print("No code found to analyze.")
            return
        
        # Create pie chart
        fig, ax = plt.subplots(figsize=(10, 8))
        
        languages = list(filtered_stats.keys())
        counts = list(filtered_stats.values())
        colors = plt.cm.Set3(np.linspace(0, 1, len(languages)))
        
        wedges, texts, autotexts = ax.pie(counts, labels=languages, autopct='%1.1f%%', 
                                          colors=colors, startangle=90)
        
        ax.set_title('Current Language Distribution by Lines of Code', 
                    fontsize=16, fontweight='bold')
        
        plt.tight_layout()
        
        # Save the chart
        output_file = output_dir / 'language_distribution.png'
        plt.savefig(output_file, dpi=300, bbox_inches='tight')
        plt.close()
        
        print(f"Language distribution chart saved to: {output_file}")
    
    def generate_placeholder_charts(self, output_dir: Path):
        """Generate placeholder charts for future metrics."""
        print("Generating placeholder charts...")
        
        # Placeholder 1: Commit frequency over time
        fig, ax = plt.subplots(figsize=(10, 6))
        ax.text(0.5, 0.5, 'Commit Frequency Over Time\n(Coming Soon)', 
                ha='center', va='center', fontsize=20, 
                bbox=dict(boxstyle='round', facecolor='lightgray', alpha=0.8))
        ax.set_xlim(0, 1)
        ax.set_ylim(0, 1)
        ax.set_title('Placeholder: Commit Frequency Analysis', fontsize=16, fontweight='bold')
        ax.axis('off')
        
        placeholder_file = output_dir / 'commit_frequency_placeholder.png'
        plt.savefig(placeholder_file, dpi=300, bbox_inches='tight')
        plt.close()
        
        # Placeholder 2: Code complexity metrics
        fig, ax = plt.subplots(figsize=(10, 6))
        ax.text(0.5, 0.5, 'Code Complexity Metrics\n(Coming Soon)', 
                ha='center', va='center', fontsize=20,
                bbox=dict(boxstyle='round', facecolor='lightblue', alpha=0.8))
        ax.set_xlim(0, 1)
        ax.set_ylim(0, 1)
        ax.set_title('Placeholder: Code Complexity Analysis', fontsize=16, fontweight='bold')
        ax.axis('off')
        
        placeholder_file = output_dir / 'code_complexity_placeholder.png'
        plt.savefig(placeholder_file, dpi=300, bbox_inches='tight')
        plt.close()
        
        # Placeholder 3: Author contribution analysis
        fig, ax = plt.subplots(figsize=(10, 6))
        ax.text(0.5, 0.5, 'Author Contribution Analysis\n(Coming Soon)', 
                ha='center', va='center', fontsize=20,
                bbox=dict(boxstyle='round', facecolor='lightgreen', alpha=0.8))
        ax.set_xlim(0, 1)
        ax.set_ylim(0, 1)
        ax.set_title('Placeholder: Author Contribution Metrics', fontsize=16, fontweight='bold')
        ax.axis('off')
        
        placeholder_file = output_dir / 'author_contributions_placeholder.png'
        plt.savefig(placeholder_file, dpi=300, bbox_inches='tight')
        plt.close()
        
        print("Placeholder charts generated.")
    
    def generate_all_charts(self, output_dir: Path):
        """Generate all project statistics charts."""
        # Ensure output directory exists
        output_dir.mkdir(parents=True, exist_ok=True)
        
        print(f"Generating project statistics in: {output_dir}")
        print(f"Repository: {self.repo_path}")
        print(f"Current branch: {self.repo.active_branch.name}")
        print("="*60)
        
        # Generate actual charts
        self.generate_loc_over_time_chart(output_dir)
        print()
        self.generate_language_distribution_chart(output_dir)
        print()
        
        # Generate placeholder charts
        self.generate_placeholder_charts(output_dir)
        print()
        
        # Generate summary report
        self.generate_summary_report(output_dir)
        
        print("="*60)
        print(f"All charts generated successfully in: {output_dir}")
    
    def generate_summary_report(self, output_dir: Path):
        """Generate a summary report of the analysis."""
        report_file = output_dir / 'project_summary.txt'
        
        with open(report_file, 'w') as f:
            f.write("Project Statistics Summary\n")
            f.write("=" * 40 + "\n\n")
            
            f.write(f"Generated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n")
            f.write(f"Repository: {self.repo_path}\n")
            f.write(f"Current branch: {self.repo.active_branch.name}\n")
            f.write(f"Current commit: {self.repo.head.commit.hexsha[:8]}\n\n")
            
            # Get current stats
            current_stats = self.analyze_commit(self.repo.head.commit)
            f.write("Current Line Counts:\n")
            for language, count in current_stats.items():
                if count > 0:
                    f.write(f"  {language.title()}: {count:,} lines\n")
            
            f.write(f"\nTotal: {sum(current_stats.values()):,} lines\n\n")
            
            f.write("Generated Charts:\n")
            f.write("- lines_of_code_over_time.png\n")
            f.write("- language_distribution.png\n")
            f.write("- commit_frequency_placeholder.png\n")
            f.write("- code_complexity_placeholder.png\n")
            f.write("- author_contributions_placeholder.png\n\n")
            
            f.write("Data Files:\n")
            f.write("- lines_of_code_over_time.csv\n")
            f.write("- project_summary.txt\n")
        
        print(f"Summary report saved to: {report_file}")

def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(
        description='Generate project statistics and charts from git history.'
    )
    parser.add_argument(
        'output_dir',
        help='Directory to save generated charts and data'
    )
    parser.add_argument(
        '--repo-path',
        default='.',
        help='Path to git repository (default: current directory)'
    )
    
    args = parser.parse_args()
    
    # Validate repository
    try:
        repo = git.Repo(args.repo_path)
    except git.exc.InvalidGitRepositoryError:
        print(f"Error: {args.repo_path} is not a valid git repository.")
        sys.exit(1)
    
    # Create output directory
    output_dir = Path(args.output_dir)
    try:
        output_dir.mkdir(parents=True, exist_ok=True)
    except Exception as e:
        print(f"Error creating output directory {output_dir}: {e}")
        sys.exit(1)
    
    # Generate statistics
    try:
        generator = ProjectStatsGenerator(args.repo_path)
        generator.generate_all_charts(output_dir)
    except Exception as e:
        print(f"Error generating statistics: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)

if __name__ == '__main__':
    main()