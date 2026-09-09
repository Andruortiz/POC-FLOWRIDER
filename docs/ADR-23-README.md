# ADR-23 — Harness listo, comparación pendiente de credenciales

El código de `src/FlowRide.Api/Notificaciones/` implementa ambos lados de la
comparación que pide [ADR-23 en Notion](https://app.notion.com/p/3d51a61595fc81e09611d69cfedf2074),
pero **hoy no se ejecutó** porque no hay credenciales de ninguno de los dos proveedores.

## Qué falta para correrla

### Twilio (más rápido de conseguir)
1. Crear cuenta en https://www.twilio.com/try-twilio (tiene sandbox gratuita para WhatsApp).
2. Activar el sandbox de WhatsApp: https://www.twilio.com/console/sms/whatsapp/sandbox — da un
   número de prueba y un código para unir el propio celular al sandbox (enviando un WhatsApp).
3. Copiar `TWILIO_ACCOUNT_SID`, `TWILIO_AUTH_TOKEN` (ambos están en el dashboard de la consola)
   y el número del sandbox a `TWILIO_WHATSAPP_FROM` (formato `+1415XXXXXXX`, sin el prefijo `whatsapp:`).

### Meta Cloud API (más lento — esto es justo lo que ADR-23 quiere re-verificar)
1. Crear una app en https://developers.facebook.com/apps con el producto "WhatsApp" agregado.
2. En "WhatsApp > Cuentas de API", Meta da un número de prueba temporal y un token de acceso
   temporal (24h) para pruebas rápidas — suficiente para este PoC, no hace falta verificación
   de negocio todavía para esta parte.
3. Copiar el token a `META_ACCESS_TOKEN` y el `Phone number ID` (no el número en sí) a
   `META_PHONE_NUMBER_ID`.
4. **Anotar cuánto tardó este paso** — es la variable que ADR-23 pide re-verificar (el
   argumento original de ADR-05 para descartar Meta fue "el onboarding es lento").

## Cómo correr la comparación una vez haya credenciales

```bash
# Twilio
curl -X POST "http://localhost:5080/notificaciones/whatsapp/enviar-prueba?proveedor=twilio" \
  -H "Content-Type: application/json" \
  -d '{"numero":"+57XXXXXXXXXX","plantilla":"Hola {0}, tu viaje esta confirmado para las {1}","parametros":["Ana","3:00pm"]}'

# Meta Cloud API (la plantilla debe existir y estar aprobada en el Administrador de WhatsApp)
curl -X POST "http://localhost:5080/notificaciones/whatsapp/enviar-prueba?proveedor=meta" \
  -H "Content-Type: application/json" \
  -d '{"numero":"57XXXXXXXXXX","plantilla":"nombre_de_la_plantilla_aprobada","parametros":["Ana","3:00pm"]}'
```

Cada llamada devuelve `Duracion` (tiempo end-to-end) y queda logueada como
`[ADR-23] Proveedor=... Exitoso=... Duracion=...ms`. Repetir varias veces cada uno y
promediar para tener un número de latencia comparable.

Para el lado del webhook entrante (RF-14/RF-16), registrar
`http://<ngrok-o-similar>/webhooks/whatsapp` como webhook en cada proveedor y mandar una
respuesta desde el WhatsApp real conectado al sandbox/número de prueba — el payload
completo queda logueado.

## Qué llenar en `docs/ADR-23-RESULTADOS.md` (crear cuando haya datos)

- Tiempo de entrega end-to-end (promedio de al menos 5 envíos por proveedor).
- Tiempo real que tomó el onboarding de cada uno (paso 2-3 de cada sección arriba).
- Costo estimado por mensaje al volumen del Escenario 2 (hasta 10.000 usuarios concurrentes) —
  usar las tablas de precios públicas de Twilio y de Meta Cloud API para el país objetivo.
- Conclusión: ¿se abre ADR-24 (Meta gana, supersede a ADR-05) o ADR-23 pasa a `rejected`
  (Twilio se confirma)?
