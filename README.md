# Database Backup Utility
![Database Backup Utility Logo](logo.png)


## Overview

The **Database Backup Utility v.1.0.0** is a versatile tool designed to simplify the process of creating and restoring backups for various database management systems (DBMS). This tool is especially useful for developers and administrators who need a reliable way to manage database backups via a command-line interface.

## Features

* **Multi-DBMS Support:** Supports MySQL, PostgreSQL, and MongoDB out of the box.
* **Automated Backups:** Easily set up automated backups with configurable options.
* **Command-Line Interface:** Manage backups and restores using simple command-line commands.
* **Flexible Storage Options:** Store backups locally or in cloud storage solutions.
* **Notifications:** Receive notifications (e.g., via Slack) upon completion of backup or restore operations.
* **Modular Architecture:** Easily extend the tool to support additional DBMS or custom storage solutions.

## Supported Databases

* **MySQL:** One of the most popular relational databases, commonly used in web applications.
* **PostgreSQL:** A robust, open-source object-relational database known for its reliability and advanced features.
* **MongoDB:** A NoSQL database oriented towards document storage, widely used for handling large volumes of unstructured data.

## Installation

1. Download the Executable: Download the latest release of `DatabaseBackupUtility.exe` from the Releases page.
2. Prepare Configuration File:
* Create a `config.json` file in the same directory as the executable.
* Example `config.json`:
```json
{
    "Database": {
        "Type": "MySql",
        "Host": "localhost",
        "DatabaseName": "mydatabase",
        "Username": "root",
        "Password": "password"
    },
    "Storage": {
        "LocalPath": "C:/Backups"
    },
    "Notifications": {
        "SlackWebhookUrl": "https://hooks.slack.com/services/T00000000/B00000000/XXXXXXXXXXXXXXXXXXXXXXXX"
    }
}
```
3. Run the Tool:
* Open a command-line interface and navigate to the directory containing `DatabaseBackupUtility.exe`.
* Execute commands to perform backups or restores.

## Usage

### Backup

To create a backup of your database:
```bash
DatabaseBackupUtility.exe --config config.json backup
```

### Restore

To restore your database from a backup:

```bash
DatabaseBackupUtility.exe --config config.json restore
```

### Configuration

* **Database Configuration:** Specifies the database type, connection details, and credentials.
* **Storage Configuration:** Defines where the backups will be stored (locally or in the cloud).
* **Notifications:** Allows you to configure notifications, such as Slack messages, to be sent upon completion of operations.

https://roadmap.sh/projects/database-backup-utility
