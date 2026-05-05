# Deployment Environment Templates

Use this file as a copy/paste reference for staging and production configuration.

## Frontend (`iot-monitoring-frontend`)

Create `Frontend/iot-monitoring-frontend/.env.staging`:

```env
REACT_APP_API_BASE_URL=https://your-staging-api.example.com/api/v1
REACT_APP_SIGNALR_HUB_URL=https://your-staging-api.example.com/monitoringhub
```

Create `Frontend/iot-monitoring-frontend/.env.production`:

```env
REACT_APP_API_BASE_URL=https://your-prod-api.example.com/api/v1
REACT_APP_SIGNALR_HUB_URL=https://your-prod-api.example.com/monitoringhub
```

## Backend (`IoTMonitoringSystem.API`) - Staging

Set these as environment variables in your staging backend host:

```env
ASPNETCORE_ENVIRONMENT=Staging

ConnectionStrings__DefaultConnection=Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True

Mqtt__Host=your-staging-mqtt.example.com
Mqtt__Port=8883
Mqtt__EnableTls=true
Mqtt__AllowUntrustedTls=false
Mqtt__Username=your-staging-mqtt-username
Mqtt__Password=your-staging-mqtt-password

Cors__AllowedOrigins__0=https://your-staging-frontend.example.com
Cors__AllowedOrigins__1=https://www.your-staging-frontend.example.com
```

If you only use one frontend URL, keep only `Cors__AllowedOrigins__0`.

## Backend (`IoTMonitoringSystem.API`) - Production

Set these as environment variables in your production backend host:

```env
ASPNETCORE_ENVIRONMENT=Production

ConnectionStrings__DefaultConnection=Server=...;Database=...;User Id=...;Password=...;Encrypt=True;TrustServerCertificate=False

Mqtt__Host=your-prod-mqtt.example.com
Mqtt__Port=8883
Mqtt__EnableTls=true
Mqtt__AllowUntrustedTls=false
Mqtt__Username=your-prod-mqtt-username
Mqtt__Password=your-prod-mqtt-password

Cors__AllowedOrigins__0=https://your-frontend.example.com
Cors__AllowedOrigins__1=https://www.your-frontend.example.com
```

## Quick Post-Deploy Validation

1. `GET https://<api-domain>/api/v1/health`
2. `GET https://<api-domain>/api/v1/health/mqtt`

Expected MQTT fields include:

- `status: "ready"`
- `subscriberConnected: true`
- `subscriberSubscribed: true`

