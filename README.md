# 📡 Communication Hub

A full-stack enterprise communication platform built for insurance adjusters to manage multi-channel communications (SMS, Email, WhatsApp) tied to insurance claims — all in one unified interface.

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│              Angular 18 Frontend  (Port 4200)            │
│   Dashboard · Claim Details · Communication Hub · UI     │
└────────────────────────┬────────────────────────────────┘
                         │  REST + SignalR (WebSockets)
                         ▼
┌─────────────────────────────────────────────────────────┐
│            ASP.NET Core 8 API  (Port 5000)              │
│   Controllers · JWT Auth · SignalR Hub · OpenAPI         │
│                                                          │
│  ┌─────────────┐  ┌──────────────┐  ┌────────────────┐  │
│  │ Application │  │ Infrastructure│  │    Domain      │  │
│  │  Interfaces │  │   Services   │  │   Entities     │  │
│  │    DTOs     │  │  EF Core / S3│  │ Clean Models   │  │
│  └─────────────┘  └──────────────┘  └────────────────┘  │
└────────────────────────┬────────────────────────────────┘
                         │  EF Core
                         ▼
┌─────────────────────────────────────────────────────────┐
│              SQL Server  (CommunicationHubDB)            │
│   Adjuster · Claim · Communication · Channel · Party     │
└─────────────────────────────────────────────────────────┘
                         │  Integrations
            ┌────────────┼───────────────┐
            ▼            ▼               ▼
        Twilio         Gmail          AWS S3
     SMS/WhatsApp   SMTP/IMAP     Attachment Store
```

The backend follows **Clean Architecture** with four layers:

| Layer | Project | Responsibility |
|---|---|---|
| Presentation | `CommunicationHub.API` | Controllers, DTOs, SignalR Hubs |
| Application | `CommunicationHub.Application` | Service Interfaces, Application DTOs |
| Infrastructure | `CommunicationHub.Infrastructure` | EF Core, external service implementations |
| Domain | `CommunicationHub.Domain` | Core entities, no dependencies |

---

## ✨ Features

### 📊 Adjuster Dashboard
- View all assigned insurance claims at a glance
- See unread message counts per claim
- Quick-navigate to any claim's communication thread

### 📨 Multi-Channel Communications
| Channel | Send | Receive | Attachments |
|---------|------|---------|-------------|
| Email (Gmail) | ✅ | ✅ (IMAP polling) | ✅ (S3) |
| SMS (Twilio) | ✅ | ✅ (Webhook) | ✅ (MMS/S3) |
| WhatsApp (Twilio) | ✅ | ✅ (Webhook) | ✅ (S3) |

### 🔔 Real-Time Updates
- SignalR hub (`/hubs/communications`) pushes new inbound messages instantly to connected clients — no polling required.

### 📁 Attachment Management
- Inbound/outbound file attachments stored in **AWS S3**
- Accessible via the `AttachmentsController`

### 🔐 Authentication & Security
- JWT Bearer token authentication
- Route guards on all protected Angular pages
- HTTP interceptor auto-attaches Bearer tokens to every API request
- Auth endpoints: `POST /api/auth/login` · `POST /api/auth/signup`

### 🏖️ Out-of-Office
- Adjusters can toggle away mode, assign a substitute, and configure custom auto-reply messages
- Auto-replies are sent automatically on incoming communications when active

---

## 🛠️ Tech Stack

### Backend
- **Runtime**: .NET 8 / ASP.NET Core
- **ORM**: Entity Framework Core (SQL Server)
- **Real-time**: SignalR
- **SMS/WhatsApp**: Twilio REST API
- **Email**: MailKit (SMTP send + IMAP listener)
- **Storage**: Amazon S3 (AWSSDK)
- **Auth**: JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`)

### Frontend
- **Framework**: Angular 18 (standalone components)
- **UI Components**: Syncfusion EJ2 Angular (Grids, Popups, RichTextEditor, etc.)
- **State/Async**: RxJS
- **API Client**: Auto-generated via `ng-openapi-gen`
- **Styling**: SCSS

---

## 📂 Project Structure

```
CommunicationHub/
├── CommunicationHub.API/           # ASP.NET Core Web API
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── CommunicationsController.cs
│   │   ├── ClaimsController.cs
│   │   ├── SmsController.cs
│   │   ├── SmsWebhookController.cs
│   │   ├── EmailController.cs
│   │   ├── WhatsAppController.cs
│   │   ├── WhatsAppWebhookController.cs
│   │   ├── AttachmentsController.cs
│   │   └── UsersController.cs
│   ├── Hubs/                       # SignalR Hub
│   ├── Security/
│   ├── DTOs/
│   └── Program.cs
│
├── CommunicationHub.Application/   # Interfaces & Application DTOs
│   ├── Interfaces/
│   └── DTOs/
│
├── CommunicationHub.Domain/        # Core domain entities
│   └── Entities/
│       ├── Adjuster.cs
│       ├── Claim.cs
│       ├── ClaimAdjuster.cs
│       ├── Communication.cs
│       ├── Channel.cs
│       ├── InvolvedParty.cs
│       └── MessageAttachment.cs
│
├── CommunicationHub.Infrastructure/ # EF Core, external services
│   ├── Data/                        # DbContext, Migrations
│   ├── Services/
│   │   ├── CommunicationService.cs
│   │   ├── MailKitEmailService.cs
│   │   ├── ImapListeningService.cs  # Background IMAP poller
│   │   ├── TwilioSmsService.cs
│   │   ├── TwilioWhatsAppService.cs
│   │   ├── S3Service.cs
│   │   ├── AutoReplyService.cs
│   │   ├── ClaimService.cs
│   │   └── AdjusterService.cs
│   └── Hubs/
│       └── MessagingHub.cs          # SignalR hub implementation
│
└── communication-hub-ui/           # Angular 18 Frontend
    └── src/app/
        ├── components/
        │   ├── adjuster-dashboard/
        │   ├── communication-hub/
        │   ├── claim-details/
        │   ├── out-of-office/
        │   ├── login/
        │   └── signup/
        ├── services/
        │   ├── auth.service.ts
        │   ├── communication.service.ts
        │   ├── claim.service.ts
        │   └── user.service.ts
        ├── guards/
        │   └── auth.guard.ts
        └── interceptors/
            └── auth.interceptor.ts
```

---

## 🚀 Getting Started

### Prerequisites

| Requirement | Version |
|---|---|
| .NET SDK | 8.0+ |
| SQL Server | 2019+ |
| Node.js | 18+ |
| Angular CLI | 18+ |
| Twilio Account | — |
| Gmail Account (App Password) | — |
| AWS Account (S3 bucket) | — |

---

### 1. Backend Setup

**Clone & configure:**
```bash
git clone https://github.com/Chetan9123/communication-hub.git
cd communication-hub
```

Edit `CommunicationHub.API/appsettings.json` and fill in your own values:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=CommunicationHubDB;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Secret": "your-super-secret-key-change-this-in-production",
    "Issuer": "CommunicationHub",
    "Audience": "CommunicationHubClient",
    "ExpiryMinutes": 60
  },
  "Twilio": {
    "AccountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "AuthToken":  "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "FromNumber": "+1XXXXXXXXXX",
    "WhatsAppNumber": "whatsapp:+14155238886"
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 465,
    "UseSsl": true,
    "Username": "you@gmail.com",
    "Password": "your-gmail-app-password"
  },
  "Imap": {
    "Host": "imap.gmail.com",
    "Port": 993,
    "UseSsl": true,
    "Username": "you@gmail.com",
    "Password": "your-gmail-app-password"
  },
  "AWS": {
    "Region": "eu-north-1",
    "BucketName": "your-s3-bucket-name",
    "AccessKey": "AKIAXXXXXXXXXXXXXXXXX",
    "SecretKey": "your-aws-secret-key"
  }
}
```

**Run migrations and start API:**
```bash
# In Visual Studio Package Manager Console
Update-Database

# Or via CLI
dotnet ef database update --project CommunicationHub.Infrastructure --startup-project CommunicationHub.API

# Start the API
dotnet run --project CommunicationHub.API
# → https://localhost:5000
```

---

### 2. Frontend Setup

```bash
cd communication-hub-ui
npm install
ng serve
# → http://localhost:4200
```

---

### 3. Twilio Webhooks

For inbound SMS/WhatsApp to reach the API, expose your local server (e.g. via [ngrok](https://ngrok.com)) and set the webhook URLs in the Twilio Console:

| Channel | Webhook URL |
|---|---|
| SMS | `https://YOUR_NGROK_URL/api/smswebhook` |
| WhatsApp | `https://YOUR_NGROK_URL/api/whatsappwebhook` |

---

## 🔌 API Reference

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/auth/login` | Login and get JWT token |
| `POST` | `/api/auth/signup` | Register a new adjuster |
| `GET` | `/api/communications/unread` | Get all unread messages |
| `GET` | `/api/communications/claim/{claimId}/party/{partyId}` | Get message thread |
| `POST` | `/api/communications/send` | Send a new communication |
| `PUT` | `/api/communications/{id}/read-status` | Mark as read/unread |
| `PUT` | `/api/communications/{id}/notes` | Update notes on a message |
| `GET` | `/api/communications/channels` | List active channels |
| `GET` | `/api/claims/assigned-to-adjuster` | Get adjuster's claims |
| `GET` | `/api/claims/{id}` | Get claim details |
| `GET` | `/api/claims/{id}/parties` | Get claim's involved parties |
| `GET` | `/api/users/dashboard` | Adjuster dashboard data |
| `GET` | `/api/users/out-of-office-status` | Get OOO status |
| `PUT` | `/api/users/out-of-office` | Set out-of-office |
| `POST` | `/api/sms` | Send SMS manually |
| `POST` | `/api/smswebhook` | Twilio inbound SMS webhook |
| `POST` | `/api/whatsapp` | Send WhatsApp message |
| `POST` | `/api/whatsappwebhook` | Twilio inbound WhatsApp webhook |
| `POST` | `/api/email/send` | Send an email |
| `GET` | `/api/attachments/{id}` | Get attachment metadata |

> OpenAPI (Swagger) docs available at `http://localhost:5000/openapi` in Development mode.

---

## ⚙️ Configuration Reference

### Enable/Disable Communication Channels (DB)

```sql
-- Enable SMS
UPDATE Channel SET IsActive = 1 WHERE Name = 'SMS'

-- Disable WhatsApp
UPDATE Channel SET IsActive = 0 WHERE Name = 'WhatsApp'
```

### Angular API Base URL

If you deploy the backend to a different host, update:
```typescript
// src/app/services/communication.service.ts
private apiUrl = 'http://localhost:5000/api';
```

---

## 🔐 Authentication Flow

```
Browser                          API
  │                               │
  │──── POST /api/auth/login ────▶│
  │◀─── { token, user } ─────────│
  │                               │
  │  (store token in localStorage)│
  │                               │
  │──── GET /api/... ────────────▶│
  │     Authorization: Bearer ... │
  │◀─── 200 OK ──────────────────│
```

**Protected Angular routes** (`/dashboard`, `/communications`, `/claim/:id`, `/out-of-office`) are guarded by `auth.guard.ts`. Unauthenticated users are redirected to `/login`.

---

## 🧪 Troubleshooting

| Problem | Solution |
|---------|----------|
| CORS errors | Verify `AllowAll` CORS policy in `Program.cs` matches frontend URL |
| Database connection failed | Check connection string, confirm SQL Server is running |
| Twilio webhooks not received | Ensure ngrok/tunnel is running and URLs are correct in Twilio Console |
| Gmail IMAP not working | Use an [App Password](https://myaccount.google.com/apppasswords), not your regular password |
| Angular won't compile | `rm -rf node_modules && npm install` in `communication-hub-ui/` |
| JWT errors | Ensure `Jwt:Secret` in `appsettings.json` is at least 32 characters |

---


## 📜 License

This project is private and intended for internal use.
