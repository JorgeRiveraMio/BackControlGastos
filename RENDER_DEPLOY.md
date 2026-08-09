# Despliegue de ControlGastos en Render

## API

1. En Render, crea **New > Web Service**, conecta el repositorio y selecciona **Docker**.
2. Usa `./Dockerfile.Api`, plan Free y health check `/api/health`.
3. Configura estas variables de entorno:

   - `ConnectionStrings__SupabaseDatabase`
   - `Supabase__Url`
   - `Supabase__SecretKey`
   - `Supabase__ComprobantesBucket`
   - `Telegram__BotToken`
   - `Telegram__BotUsername`
   - `Telegram__WebhookSecret`
   - `ASPNETCORE_ENVIRONMENT=Production`

Cuando Render asigne la URL, prueba `https://<API_RENDER>/api/health`. El endpoint es anónimo. También puede comprobarse `/api/health/database` si la conexión está configurada.

## Web

1. Crea otro **Web Service** de Docker con `./Dockerfile.Web`.
2. Configura:

   - `SupabaseAuth__Url`
   - `SupabaseAuth__PublishableKey`
   - `Api__BaseUrl=https://<API_RENDER>`
   - `ASPNETCORE_ENVIRONMENT=Production`

`Api__BaseUrl` se configura manualmente después de conocer la URL pública de la API; no se incluye una URL fija en el repositorio.

## Telegram después del deploy

Con una API HTTPS pública, registra el webhook manualmente:

```powershell
curl -X POST "https://api.telegram.org/bot<TOKEN>/setWebhook" `
  -d "url=https://<API_RENDER>/api/telegram/webhook" `
  -d "secret_token=<WEBHOOK_SECRET>"
```

Verifica con `https://api.telegram.org/bot<TOKEN>/getWebhookInfo` y elimina el webhook, si fuera necesario, con `https://api.telegram.org/bot<TOKEN>/deleteWebhook`. Usa siempre valores reales solo en una terminal segura; no los guardes en archivos del repositorio.

## Notas de Render Free

Los servicios pueden suspenderse por inactividad y el primer request puede tardar mientras inicia. API y Web pueden dormir independientemente. La sesión MVC está en memoria: si la instancia Web se reinicia, el usuario deberá iniciar sesión de nuevo. Los datos permanecen en Supabase.

## Validación local

Si Docker Desktop está instalado:

```powershell
docker build -f Dockerfile.Api -t controlgastos-api .
docker build -f Dockerfile.Web -t controlgastos-web .
```

Los contenedores escuchan en `0.0.0.0:$PORT`; sin `PORT`, usan `10000`.
