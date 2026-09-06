# GDRVault

![GDRVault](GDRVault/GDRVault.ico)

**GDRVault** is a secure Windows password manager designed to protect your credentials with modern encryption and provide convenient browser autofill support.

## Features

- 🔐 Secure password vault
- 🛡️ Argon2id key derivation
- 🔒 AES-GCM authenticated encryption
- 🗄️ Local SQLite database
- 🔑 Master password protection
- 🔄 Change master password
- 🎲 Secure password generator
- 🌐 Browser autofill support
- 🧩 Chrome and Microsoft Edge extensions
- 🔗 Native Messaging integration
- 🖥️ Native Windows desktop application
- 💾 Local-first architecture

## Security

GDRVault uses modern cryptographic technologies to protect the vault.

### Key derivation

The master password is processed using **Argon2id**, a password hashing algorithm designed to make brute-force attacks significantly more expensive.

### Encryption

Vault data is protected using **AES-GCM**, providing both confidentiality and authentication.

The vault database is stored locally on the user's computer.

## Architecture

GDRVault is divided into several projects:

```text
GDRVault
│
├── GDRVault
│   └── Windows WPF desktop application
│
├── GDRVault.Core
│   └── Core vault models and document structures
│
├── GDRVault.Security
│   └── Cryptography and security services
│
├── GDRVault.Storage
│   └── SQLite database and vault storage
│
├── GDRVault.Bridge
│   └── Native Messaging bridge for browser autofill
│
└── GDRVault.Security.Tests
    └── Security and service tests

Browser Autofill

GDRVault Autofill connects the browser extension with the desktop application through Native Messaging.

Browser Extension
       │
       ▼
Native Messaging
       │
       ▼
GDRVault.Bridge
       │
       ▼
Named Pipe
       │
       ▼
GDRVault Desktop App
       │
       ▼
Encrypted Vault

The desktop application must be running with the vault unlocked for autofill functionality to access credentials.

Supported Browsers
Google Chrome
Microsoft Edge

The GDRVault Autofill browser extension is a companion to the GDRVault desktop application and is not intended to operate as a standalone password manager.

Requirements
Windows
.NET 10 compatible environment
Google Chrome and/or Microsoft Edge for browser autofill

The distributed Windows application is published as a self-contained application.

Installation
Desktop application

Download the latest GDRVault installer from the project's Releases page and run:

GDRVault-Setup.exe

The installer configures:

GDRVault desktop application
GDRVault Bridge
Native Messaging hosts
Chrome Native Messaging registration
Microsoft Edge Native Messaging registration
Start Menu shortcuts
Optional desktop shortcut
Browser extension

Install GDRVault Autofill from the official browser extension store.

The browser extension requires the GDRVault desktop application and its Native Messaging Bridge.

Development

Clone the repository:

git clone https://github.com/GaetanoDeRoberto/GDRVault.git

GDRVault.slnx

Build the solution in Visual Studio.

The solution contains the desktop application, core libraries, security components, storage layer, browser bridge and security tests.

Project Status

GDRVault is currently under active development.

The current version includes:

Windows desktop password manager
Encrypted local vault
Password generator
Credential management
Master password management
Chrome autofill
Microsoft Edge autofill
Windows installer
Website

Official website:

https://www.gdrdesign.it/

Privacy

Privacy information:

https://www.gdrdesign.it/privacy-gdrvault

License

License information will be added as the project develops.

GDRVault — Secure your credentials. Keep control of your data.


### 2. Salva il file

Poi torna nel PowerShell che hai già aperto e lancia:

```powershell
git status