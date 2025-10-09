# OAuth Authentication Setup Guide

This guide explains how to configure and use OAuth authentication with Keycloak for the WNAB application.

## Overview

The WNAB application has been configured with OAuth 2.0 / OpenID Connect authentication using Keycloak as the identity provider. This provides:

- **Secure Authentication**: Industry-standard OAuth 2.0 authorization code flow
- **Single Sign-On (SSO)**: Unified authentication across Web and MAUI apps
- **Token-based Security**: JWT tokens for API authorization
- **Automatic User Provisioning**: Users are created in the database on first login

## Architecture

### Components

1. **Keycloak Server**: Identity provider at `https://engineering.snow.edu/auth`
   - Realm: `SnowCollege`
   - Handles user authentication and token issuance

2. **API (WNAB.API)**: Resource server
   - Validates JWT Bearer tokens
   - Protects all endpoints with authorization
   - Auto-provisions users on first access

3. **Web App (WNAB.Web)**: Blazor Server application
   - Uses OpenID Connect with authorization code flow
   - Stores tokens in secure cookies
   - Automatically attaches tokens to API requests

4. **MAUI App (WNAB.Maui)**: Mobile/desktop application
   - Uses OAuth 2.0 with PKCE for native apps
   - Stores tokens in device secure storage
   - Supports token refresh

## Keycloak Configuration

### Required Clients

You need to create three clients in your Keycloak realm:

#### 1. API Client (`wnab-api`)
- **Client ID**: `wnab-api`
- **Access Type**: bearer-only
- **Purpose**: Validates tokens for API requests

#### 2. Web Client (`wnab-web`)
- **Client ID**: `wnab-web`
- **Client Protocol**: openid-connect
- **Access Type**: confidential
- **Valid Redirect URIs**:
  - `https://localhost:5001/signin-oidc` (development)
  - `https://your-production-domain.com/signin-oidc` (production)
- **Valid Post Logout Redirect URIs**:
  - `https://localhost:5001/signout-callback-oidc` (development)
  - `https://your-production-domain.com/signout-callback-oidc` (production)
- **Web Origins**: `+` (to allow CORS from redirect URIs)

**Important**: Copy the client secret from the Credentials tab

#### 3. MAUI Client (`wnab-maui`)
- **Client ID**: `wnab-maui`
- **Client Protocol**: openid-connect
- **Access Type**: public
- **Valid Redirect URIs**: `wnab://callback`
- **Valid Post Logout Redirect URIs**: `wnab://callback`
- **Advanced Settings**:
  - Proof Key for Code Exchange (PKCE): Enabled
  - PKCE Code Challenge Method: S256

### Realm Settings

Ensure your realm has these settings:
- **Login with email**: Enabled
- **Email as username**: Optional (depends on your preference)
- **Require SSL**: All requests (for production)

## Application Configuration

### API Configuration (WNAB.API)

Update `appsettings.json`:

```json
{
  "Keycloak": {
    "Authority": "https://engineering.snow.edu/auth/realms/SnowCollege",
    "Audience": "wnab-api",
    "RequireHttpsMetadata": true,
    "ValidateAudience": true,
    "ValidateIssuer": true
  }
}
```

### Web App Configuration (WNAB.Web)

Update `appsettings.json` with your client secret:

```json
{
  "Keycloak": {
    "Authority": "https://engineering.snow.edu/auth/realms/SnowCollege",
    "ClientId": "wnab-web",
    "ClientSecret": "YOUR_CLIENT_SECRET_HERE",
    "RequireHttpsMetadata": true,
    "ResponseType": "code",
    "SaveTokens": true,
    "GetClaimsFromUserInfoEndpoint": true,
    "Scopes": ["openid", "profile", "email"]
  }
}
```

**Security Note**: Never commit the client secret to version control. Use environment variables or secret management in production:

```bash
# Set via environment variable
export Keycloak__ClientSecret="your-secret-here"

# Or use User Secrets in development
dotnet user-secrets set "Keycloak:ClientSecret" "your-secret-here" --project src/WNAB.Web
```

### MAUI App Configuration (WNAB.Maui)

The `appsettings.json` file has been created with default settings:

```json
{
  "Keycloak": {
    "Authority": "https://engineering.snow.edu/auth/realms/SnowCollege",
    "ClientId": "wnab-maui",
    "RedirectUri": "wnab://callback"
  },
  "ApiBaseUrl": "https://localhost:7077/"
}
```

#### Platform-Specific Setup

**Android**:
Add to `AndroidManifest.xml`:
```xml
<activity android:name="microsoft.identity.client.BrowserTabActivity">
  <intent-filter>
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />
    <data android:scheme="wnab" android:host="callback" />
  </intent-filter>
</activity>
```

**iOS**:
Add to `Info.plist`:
```xml
<key>CFBundleURLTypes</key>
<array>
  <dict>
    <key>CFBundleURLSchemes</key>
    <array>
      <string>wnab</string>
    </array>
    <key>CFBundleURLName</key>
    <string>WNAB OAuth Callback</string>
  </dict>
</array>
```

**Windows**: No additional configuration required.

## Database Migration

A new migration is required for the `KeycloakSubjectId` field:

```bash
# Create migration
dotnet ef migrations add AddKeycloakSubjectId --project src/WNAB.Logic --startup-project src/WNAB.API

# Apply migration (or it will auto-apply on API startup)
dotnet ef database update --project src/WNAB.Logic --startup-project src/WNAB.API
```

## Usage

### Web Application

1. Navigate to the application
2. Click "Login" in the top-right corner
3. Redirected to Keycloak login page
4. After successful login, redirected back to app
5. User info displayed in top-right
6. All API calls automatically include authentication token

### MAUI Application

The authentication service is available via dependency injection:

```csharp
public class MyViewModel
{
    private readonly IAuthenticationService _authService;

    public MyViewModel(IAuthenticationService authService)
    {
        _authService = authService;
    }

    public async Task LoginAsync()
    {
        var success = await _authService.LoginAsync();
        if (success)
        {
            // Navigate to main app
        }
    }

    public async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        // Navigate to login screen
    }
}
```

## API Endpoints

### `/api/me` - Current User Information

Returns information about the currently authenticated user. Creates user record on first access.

**Response**:
```json
{
  "id": 1,
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "keycloakSubjectId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "isActive": true
}
```

## Security Features

### Token Management

- **Access Tokens**: Short-lived (typically 5-15 minutes)
- **Refresh Tokens**: Used to obtain new access tokens without re-authentication
- **Token Storage**:
  - Web: Encrypted cookies
  - MAUI: Platform secure storage (Keychain/KeyStore)

### API Security

- All endpoints except `/` require authentication
- JWT tokens validated on every request
- Token expiration checked automatically
- Invalid tokens return 401 Unauthorized

### User Provisioning

- Users automatically created on first authentication
- Keycloak subject ID used as unique identifier
- User info updated from token claims on each login
- Email, first name, and last name synchronized

## Troubleshooting

### Common Issues

1. **"Authentication Failed" Error**
   - Verify Keycloak server is accessible
   - Check client configuration matches settings
   - Ensure redirect URIs are correctly configured

2. **401 Unauthorized on API Calls**
   - Token may be expired - try re-authenticating
   - Check API configuration matches Keycloak realm
   - Verify token is being sent in Authorization header

3. **MAUI App Won't Authenticate**
   - Ensure platform-specific manifests are configured
   - Check redirect URI scheme is registered
   - Verify Keycloak public client settings

4. **"Invalid Client Secret" Error**
   - Regenerate secret in Keycloak
   - Update appsettings.json or environment variable
   - Clear browser cache/cookies

### Debug Logging

Enable detailed authentication logging in development:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore.Authentication": "Debug",
      "Microsoft.AspNetCore.Authorization": "Debug"
    }
  }
}
```

## Production Considerations

1. **HTTPS Required**: All endpoints must use HTTPS in production
2. **Client Secrets**: Use environment variables or Azure Key Vault
3. **CORS Configuration**: Configure appropriate CORS policies
4. **Token Expiration**: Monitor and adjust token lifetimes
5. **Logging**: Implement security event logging
6. **Rate Limiting**: Consider rate limiting authentication endpoints

## Testing

### Manual Testing Checklist

**Web App**:
- [ ] Can login successfully
- [ ] Username displayed after login
- [ ] Can logout successfully
- [ ] Redirected to login when accessing protected pages while not authenticated
- [ ] API calls succeed with authentication

**MAUI App**:
- [ ] Can login on Android
- [ ] Can login on iOS
- [ ] Can login on Windows
- [ ] Tokens persist across app restarts
- [ ] Can logout successfully
- [ ] Token refresh works correctly

**API**:
- [ ] Unauthenticated requests return 401
- [ ] Valid tokens allow access
- [ ] Expired tokens rejected
- [ ] `/api/me` creates new users correctly
- [ ] User info updated on repeated logins

## Support

For issues related to:
- **Keycloak configuration**: Contact your Keycloak administrator
- **Application setup**: See this documentation or project README
- **Development questions**: Open an issue on the project repository
