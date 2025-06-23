# Jellyfin MinIO Backup Plugin

A backup solution for Jellyfin that automatically backs up your server data to MinIO object storage.

## Overview

The MinIO Backup Plugin provides automated, scheduled backups of your Jellyfin server data to MinIO-compatible object storage services (MinIO, AWS S3, etc.). It offers granular control over what gets backed up and includes automatic cleanup of old backups based on your retention policy.

## Features

- **Selective Backup**: Choose which Jellyfin folders to backup
- **Automated Scheduling**: Built-in scheduled tasks for regular backups
- **Retention Management**: Automatic cleanup of old backups
- **MinIO/S3 Compatible**: Works with MinIO, AWS S3, and other S3-compatible services
- **SSL Support**: Secure connections with SSL/TLS
- **Web UI**: Easy configuration through Jellyfin's web interface
- **Database Consistency**: Proper SQLite database checkpointing for consistent backups

## Backup Options

The plugin can backup the following Jellyfin folders:

- **Config** (Essential): Server configuration files and settings
- **Plugins** (Essential): Installed plugins and their configurations
- **Data** (Essential): Database files and user data
- **Logs** (Optional): Server log files
- **Metadata** (Optional): Movie/TV metadata and artwork
- **Root** (Optional): Root directory contents

## Installation

### Prerequisites

- Jellyfin Server 10.10.0 or higher
- Access to MinIO or S3-compatible storage
- .NET 8.0 runtime

### Kubernetes Deployment

For Kubernetes deployments, use the included deployment script:

```bash
# Build the plugin
dotnet build --configuration Release

# Deploy to Kubernetes
chmod +x deploy-plugin.sh
./deploy-plugin.sh
```

The script will:
- Automatically detect your Jellyfin pod
- Copy plugin files to the correct directory
- Restart the pod to load the plugin

## Configuration

### MinIO/S3 Settings

1. Navigate to **Dashboard → Plugins → MinIO Backup**
2. Configure the following settings:

| Setting | Description | Required |
|---------|-------------|----------|
| MinIO Endpoint | Your MinIO server endpoint (e.g., `minio.example.com:9000`) | Yes |
| Access Key | MinIO access key | Yes |
| Secret Key | MinIO secret key | Yes |
| Bucket Name | Bucket for storing backups (default: `jellyfin`) | Yes |
| Region | MinIO region (leave empty for MinIO) | No |
| Use SSL | Enable SSL/TLS encryption | No |
| Retention Days | How long to keep backups (default: 30 days) | Yes |

### Backup Folders

Select which folders to include in regular backups:

- **Essential folders** (Config, Plugins, Data): Recommended for all backups
- **Optional folders** (Logs, Metadata, Root): Can be large, backup selectively

### Exclude Patterns

Specify file patterns to exclude from backups (one per line):
```
transcodes/*
cache/*
*.tmp
*.log
```

## Scheduled Tasks

The plugin provides three scheduled tasks:

### 1. MinIO Backup (Daily at 2 AM)
- Creates regular backups based on your folder selection
- Respects the backup folder configuration

### 2. MinIO Full Backup (Weekly on Sunday at 3 AM)
- Creates complete backups of ALL folders
- Ignores folder selection settings
- Recommended for comprehensive weekly backups

### 3. MinIO Backup Cleanup (Daily at 4 AM)
- Removes old backups based on retention policy
- Frees up storage space automatically

## Manual Backup

You can trigger backups manually from:
**Dashboard → Scheduled Tasks → [Select Task] → Run Now**

## Backup Structure

Backups are stored in MinIO with the following structure:
```
bucket/
└── backups/
    ├── full_backup_20250623_020000.zip
    ├── complete_backup_20250622_030000.zip
    └── ...
```

Each backup is a compressed ZIP file containing the selected Jellyfin folders.

## Troubleshooting

### Common Issues

**Plugin not loading**
- Ensure Jellyfin version is 10.10.0 or higher
- Check that all required DLL files are present
- Verify file permissions

**MinIO connection failed**
- Verify endpoint URL (include port if needed)
- Check access key and secret key
- Ensure bucket exists or plugin has permission to create it
- For self-signed certificates, SSL validation is automatically bypassed

**Backup task failed**
- Check Jellyfin logs for detailed error messages
- Verify sufficient disk space for temporary files
- Ensure MinIO credentials have write permissions

### Logs

Check Jellyfin logs for detailed information:
```bash
# Docker/Kubernetes
kubectl logs <jellyfin-pod> -n <namespace>

# Standard installation
tail -f /var/log/jellyfin/jellyfin.log
```

## Development

### Building from Source

```bash
# Clone repository
git clone <repository-url>
cd jellyfin-minio-backup-plugin

# Build plugin
dotnet build --configuration Release

# Output files will be in bin/Release/net8.0/
```

### Project Structure

```
├── Configuration/           # Plugin configuration and web UI
├── ScheduledTasks/         # Backup and cleanup tasks
├── Services/               # Core backup service logic
├── manifest.json           # Plugin metadata
└── Plugin.cs              # Main plugin class
```

## Dependencies

- **Jellyfin.Controller** (10.10.7): Core Jellyfin APIs
- **Minio** (6.0.1): MinIO client library
- **Microsoft.Data.Sqlite** (8.0.8): SQLite database operations
- **CommunityToolkit.HighPerformance** (8.3.0): Performance optimizations
- **System.Reactive** (6.0.0): Reactive extensions

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the [MIT License](LICENSE).

## Support

- **Issues**: Report bugs and feature requests via GitHub Issues
- **Documentation**: Check the Jellyfin plugin documentation
- **Community**: Join the Jellyfin community forums

## Changelog

### Version 1.0.0.1 (Initial Release)
- Core backup functionality
- MinIO/S3 integration
- Scheduled backup tasks
- Web UI configuration
- Automatic cleanup
- Kubernetes deployment support