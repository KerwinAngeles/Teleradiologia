# Despliegue en Coolify

Guía para poner el stack en una VPS con Coolify ya instalado. El resultado: un solo dominio
público servido por nginx, con Postgres, Orthanc y GoTrue solo en la red interna de Docker.

## 0. Antes de empezar

- **DNS.** Un registro `A` de `teleradiologia.tu-dominio.com` apuntando a la IP de la VPS.
  Sin esto Coolify no puede emitir el certificado.
- **SMTP real.** Hace falta host, puerto **465**, usuario y contraseña. El puerto 587 no sirve:
  `SmtpEmailSender` solo implementa TLS directo, no STARTTLS.
- **Disco.** Un TC de 361 cortes ronda los 180 MB en el volumen de Orthanc. Calculá el volumen
  esperado antes de elegir el plan de la VPS.
- **RAM.** Si Coolify compila en la VPS, la etapa de build baja el SDK de .NET (>1 GB) y corre
  Vite. Con menos de 4 GB conviene compilar en CI (ver el paso 6).

## 1. Generar los secretos

En tu máquina (o en la VPS por SSH). Guardalos en un gestor de contraseñas: varios no se pueden
recuperar después.

```bash
# Contraseñas de servicio
openssl rand -base64 24   # POSTGRES_PASSWORD
openssl rand -base64 24   # ORTHANC_PASSWORD
openssl rand -base64 24   # SUPABASE_DB_PASSWORD

# Secreto con el que GoTrue firma los tokens y la API los valida
openssl rand -base64 48   # SUPABASE_JWT_SECRET
```

### La clave de firma de informes

Es el valor más delicado del stack. Con ella se firman los informes radiológicos; **si se pierde
o se cambia, todos los informes firmados antes dejan de verificar**. Respaldala fuera del
servidor antes de desplegar.

```bash
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out firma-prod.pem
cat firma-prod.pem
```

Ese PEM completo, con las líneas `BEGIN`/`END`, es el valor de `FIRMA_CLAVE_PRIVADA_PEM`.

### La clave `service_role`

Es un JWT firmado con `SUPABASE_JWT_SECRET` que le da a la API permisos de superusuario sobre
GoTrue (crear y borrar cuentas). Si regenerás el secreto, hay que regenerar también esta clave o
el alta de usuarios devuelve 401.

```bash
export SUPABASE_JWT_SECRET='...el valor generado arriba...'

b64() { openssl base64 -A | tr '+/' '-_' | tr -d '='; }
h=$(printf '%s' '{"alg":"HS256","typ":"JWT"}' | b64)
p=$(printf '%s' "{\"iss\":\"supabase\",\"role\":\"service_role\",\"iat\":$(date +%s),\"exp\":$(($(date +%s)+315360000))}" | b64)
s=$(printf '%s' "$h.$p" | openssl dgst -sha256 -hmac "$SUPABASE_JWT_SECRET" -binary | b64)
echo "$h.$p.$s"
```

## 2. Crear el recurso en Coolify

1. **Projects → New → Resource → Docker Compose** (origen: *Public/Private Repository*).
2. Repositorio: este repo. Si es privado, conectá la GitHub App de Coolify o cargá una deploy key.
3. Branch: `main`.
4. **Docker Compose Location:** `docker-compose.prod.yml` (no el `docker-compose.yml`, que es de
   desarrollo y publica puertos).
5. Guardá sin desplegar todavía.

## 3. Cargar las variables de entorno

En la pestaña **Environment Variables** del recurso, con `.env.prod.example` como lista de
control. Todas las que el compose marca con `:?` son obligatorias: si falta una, el arranque
falla con un mensaje que la nombra, en vez de levantar algo inseguro.

`FIRMA_CLAVE_PRIVADA_PEM` es multilínea: activá el interruptor **Multiline** de esa variable
antes de pegar el PEM.

`APP_URL` va con esquema y **sin barra final** — por ejemplo `https://teleradiologia.tu-dominio.com`.
Lo usan CORS y los enlaces de recuperación de contraseña que manda GoTrue.

## 4. Asignar el dominio

En **Configuration → Domains**, poné el dominio en el servicio **`frontend`**, puerto **80**.

Ningún otro servicio lleva dominio. Orthanc trae un visor web que dejaría navegar estudios de
pacientes sin pasar por la API ni por la auditoría: por eso no publica puertos y no debe recibir
un dominio.

Coolify (Traefik) termina el TLS y reenvía HTTP plano; `nginx.conf` ya reenvía el
`X-Forwarded-Proto` original para que la API no entre en bucle de redirección.

## 5. Primer deploy

Dale **Deploy** y seguí los logs. El API va a arrancar y quedar *unhealthy*: es lo esperado, la
base todavía está vacía. Eso se resuelve en el paso 7.

## 6. Opcional — compilar en CI en vez de en la VPS

Ya está armado en `.github/workflows/deploy.yml`: construye las dos imágenes, las publica en GHCR
y le avisa a Coolify. Para usarlo:

1. En GitHub → *Settings → Secrets and variables → Actions*, cargá `COOLIFY_WEBHOOK_URL`
   (Coolify → recurso → Webhooks) y `COOLIFY_TOKEN` (Coolify → Keys & Tokens → API tokens).
2. En Coolify, agregá `IMAGE_PREFIX=ghcr.io/kerwinangeles/teleradiologia` e `IMAGE_TAG=latest`.
   **Todo en minúsculas**: GHCR rechaza mayúsculas en el nombre de la imagen.
3. Si el repo es privado, dale acceso a la VPS al registro (`docker login ghcr.io`) o hacé
   públicos los paquetes.

A partir de ahí, cada push a `main` compila en Actions y el deploy en la VPS es solo un `pull`.

## 7. Crear el esquema de la base

El proyecto es **database-first**: la API no corre migraciones ni siembra nada al arrancar. El
esquema se aplica a mano una sola vez.

Por SSH en la VPS, con el stack ya levantado:

```bash
# El nombre exacto lo da: docker ps --format '{{.Names}}' | grep postgres
CONTENEDOR=$(docker ps --format '{{.Names}}' | grep -m1 'postgres')

docker cp db/schema.sql "$CONTENEDOR":/tmp/schema.sql
docker exec -i "$CONTENEDOR" psql -U teleradiologia -d teleradiologia -f /tmp/schema.sql
```

Después de esto el healthcheck del API pasa a *healthy*.

## 8. Crear el primer administrador

No hay seed: el alta normal pasa por la pantalla de registro y necesita que un Admin la apruebe,
así que el primer Admin se crea a mano. Son dos pasos, porque la identidad vive en GoTrue y el
perfil (rol, estado) en nuestra base.

**a. La credencial, en GoTrue:**

```bash
AUTH=$(docker ps --format '{{.Names}}' | grep -m1 'supabase-auth')

docker exec -i "$AUTH" wget -qO- \
  --header="Authorization: Bearer $SUPABASE_SERVICE_ROLE_KEY" \
  --header='Content-Type: application/json' \
  --post-data='{"email":"admin@tu-dominio.com","password":"UNA-CONTRASEÑA-LARGA","email_confirm":true}' \
  http://127.0.0.1:9999/admin/users
```

**b. El perfil, en nuestra base:**

```sql
INSERT INTO "Usuarios" ("Id", "NombreCompleto", "Email", "Rol", "EstadoAcceso",
                        "Proveedor", "ProveedorUserId", "CreatedAt")
VALUES (gen_random_uuid(), 'Nombre Apellido', 'admin@tu-dominio.com', 'Admin', 'Aprobado',
        'supabase', NULL, now());
```

`ProveedorUserId` queda en `NULL` a propósito: se completa solo en el primer login, cuando la API
vincula el `sub` del token con el perfil. El email tiene que coincidir exactamente con el del
paso *a*.

Entrá al dominio, iniciá sesión con esa cuenta y desde ahí ya podés dar de alta hospitales y
aprobar el resto de los usuarios.

## 9. Respaldos

Tres cosas hay que respaldar, y ninguna se respalda sola:

| Qué | Dónde | Si se pierde |
|---|---|---|
| `FIRMA_CLAVE_PRIVADA_PEM` | fuera del servidor | los informes firmados no verifican nunca más |
| Volumen `postgres-data` | `pg_dump` periódico | se pierden estudios, informes y auditoría |
| Volumen `orthanc-data` | copia del volumen | se pierden las imágenes DICOM |

```bash
docker exec "$CONTENEDOR" pg_dump -U teleradiologia teleradiologia | gzip > respaldo-$(date +%F).sql.gz
```
