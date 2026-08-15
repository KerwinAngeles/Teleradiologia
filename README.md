# Teleradiología

Plataforma para que un radiólogo remoto reciba, visualice e informe estudios DICOM enviados por un hospital, sin necesidad de estar físicamente en el sitio.

Flujo objetivo del MVP: **hospital sube un estudio → aparece en la cola del radiólogo → el radiólogo lo visualiza y redacta el informe → el hospital recibe la notificación con el informe listo.** Todo con control de acceso por rol y bitácora de auditoría (quién vio qué estudio y cuándo).

## Arquitectura

```
Hospital → DICOM → Orthanc (mini-PACS) → API .NET (ASP.NET Core) → Vue 3 (visor DICOM embebido)
                                                │
                                          PostgreSQL (pacientes, estudios, informes, auditoría)
```

| Pieza | Elección | Por qué |
|---|---|---|
| Backend | ASP.NET Core Web API (.NET 10), arquitectura Onion | Ver [Arquitectura del backend](#arquitectura-del-backend). |
| DICOM / mini-PACS | [Orthanc](https://www.orthanc-server.com/) | Recibe, indexa y expone DICOM vía REST/DICOMweb — no reinventamos el manejo de DICOM. |
| Frontend | Vue 3 + TypeScript + Tailwind CSS v4 | Diseño propio inspirado en dashboards SaaS tipo [Able Pro](https://themeforest.net/item/able-pro-bootstrap-admin-dashboard-template/50170229) (nav horizontal, cards, indigo) — no es su código, es un template pago. |
| Visor de imágenes | Render de Orthanc (`/instances/{id}/rendered`), proxeado por la Api | No es Cornerstone/OHIF — para diagnóstico real hay que migrar a eso; ver nota en [Visor DICOM](#visor-dicom). |
| Base de datos | PostgreSQL (EF Core / Npgsql) | — |
| Autenticación | ASP.NET Core Identity + JWT propio | Identity resuelve hashing, política de contraseña y lockout — no lo reinventamos. Emitimos nuestro propio JWT para controlar claims y loguear cada login. |
| Email | SMTP vía [MailKit](https://github.com/jstedfast/MailKit) | [Mailpit](https://github.com/axllent/mailpit) en dev — atrapa los emails localmente, nada sale a internet. |
| Infra local | Docker Compose | Postgres + Orthanc + Mailpit + API con un solo comando. |

### Arquitectura del backend

`Teleradiologia.Api` quedó dividido en 4 proyectos (Onion — las dependencias apuntan hacia adentro, el Domain no depende de nada):

```
Teleradiologia.Domain           (sin dependencias)
        ↑
Teleradiologia.Application       (interfaces/puertos + casos de uso + DTOs)
        ↑
Teleradiologia.Infrastructure     (EF Core, Identity, JWT — implementa los puertos de Application)
        ↑
Teleradiologia.Api                 (controllers + Program.cs — composition root)
```

- **Domain**: entidades (`Paciente`, `Estudio`, `Informe`, `AuditLog`) y enums. No conoce EF Core, ASP.NET ni Identity. Ojo: no hay entidad `Usuario` acá — Identity la maneja en Infrastructure; Domain solo guarda el `Guid` del usuario como FK (p. ej. `Estudio.RadiologoAsignadoId`).
- **Application**: casos de uso (`AuthService`, `UsuarioService`), interfaces que Infrastructure implementa (`IIdentityService`, `IJwtTokenService`, `IAuditLogRepository`, `IUnitOfWork`, `IDatabaseHealthCheck`) y las excepciones/DTOs del dominio de aplicación. No conoce EF Core ni Identity — solo sus propias abstracciones.
- **Infrastructure**: `AppDbContext` (EF Core + `IdentityDbContext`), `ApplicationUser`/`IdentityService` (adaptador sobre `UserManager`/`RoleManager`), emisión de JWT, seed del Admin inicial. Acá vive todo lo que sabe de Postgres/Identity.
- **Api**: controllers finos (sin lógica de negocio, sin try/catch — las excepciones de Application se traducen a HTTP en un único `IExceptionHandler`), `Program.cs` como composition root.

## Estado actual: esqueleto

Este repo por ahora **no tiene features clínicas** — es el esqueleto validado end-to-end:

- ✅ API .NET arriba, conectada a Postgres (`GET /api/health`)
- ✅ Orthanc arriba y accesible (mini-PACS, protegido con usuario/clave)
- ✅ Frontend Vue arriba, consumiendo la API a través del proxy de Vite
- ✅ Modelo de datos (`Paciente`, `Estudio`, `Informe`, `AuditLog`) + migración inicial
- ✅ Autenticación (Identity + JWT) y roles (técnico / radiólogo / admin)
- ✅ Backend reorganizado en arquitectura Onion (Domain / Application / Infrastructure / Api)
- ✅ Subida de estudios DICOM (`POST /api/estudios` → Orthanc + metadata en Postgres)
- ✅ Worklist del radiólogo (tomar un estudio Pendiente, filtrar por estado / "asignado a mí")
- ✅ Redacción/firma de informe, con adendas (correcciones post-firma) y notificación por email al técnico
- ✅ Visor DICOM en el navegador + auditoría de vistas (`VioEstudio` al abrir un estudio)
- ⬜ Frontend: subida de estudios, redacción de informe, alta de usuarios (hoy solo worklist + visor — se probaron con `curl`/Postman)

### Autenticación

No hay auto-registro: un **Admin** da de alta a técnicos y radiólogos vía `POST /api/usuarios`. Al arrancar por primera vez (base sin usuarios), la API siembra un Admin con las credenciales de `Seed:AdminEmail` / `Seed:AdminPassword`.

El storage de usuarios, el hash de contraseña y el lockout por intentos fallidos (5 intentos → 15 min bloqueado) los maneja **ASP.NET Core Identity** — no hay código propio para eso. La política de contraseña (mínimo 8 caracteres, al menos un dígito y una minúscula) se configura en `Teleradiologia.Infrastructure/DependencyInjection.cs`.

```
POST /api/auth/login        { "email": "...", "password": "..." } → { token, expiresAt, usuario }
GET  /api/auth/me           (requiere Authorization: Bearer <token>)
GET  /api/usuarios          (solo Admin)
POST /api/usuarios          (solo Admin) { nombreCompleto, email, password, rol: "Tecnico"|"Radiologo"|"Admin" }
```

### Subida de estudios DICOM

```
GET  /api/estudios                (cualquier usuario autenticado) — ?estado=Pendiente&asignadoAMi=true
POST /api/estudios                (solo Técnico o Admin) — multipart/form-data: Archivos[] (uno o más .dcm) + HospitalOrigen
POST /api/estudios/{id}/tomar     (solo Radiólogo) — se autoasigna el estudio, Pendiente → EnInforme
```

El técnico sube los archivos DICOM de un estudio (una o varias instancias/slices); la Api los reenvía a Orthanc tal cual y lee la metadata (paciente, `StudyInstanceUID`, modalidad, descripción, fecha) de los tags del propio DICOM — no hay formulario aparte para eso. Es idempotente: volver a subir el mismo `StudyInstanceUID` no duplica el estudio (devuelve el existente con `200`, en vez de `201`).

La worklist es el mismo `GET /api/estudios` con filtros: `estado=Pendiente` para la cola disponible, `asignadoAMi=true` para "mis estudios en curso" (se pueden combinar). "Tomar" un estudio es atómico y exclusivo — si dos radiólogos lo intentan a la vez, el segundo recibe `409` (el estudio ya no está en `Pendiente` ni sin asignar).

> Simplificación del MVP: `Paciente.DocumentoIdentidad` se llena con el tag DICOM `PatientID`, que en la práctica es el número de historia clínica del hospital de origen, no necesariamente un documento de identidad nacional — cuando haya más de un hospital esto va a necesitar un matching de pacientes más real.

### Informes (redacción, firma, adendas)

```
GET  /api/estudios/{estudioId}/informes    (cualquier autenticado) — historial: original + adendas
POST /api/estudios/{estudioId}/informes    (Radiólogo asignado) — crea el borrador inicial
PUT  /api/informes/{id}                    (autor) — edita mientras esté en Borrador
POST /api/informes/{id}/firmar             (autor) — Borrador → Firmado, inmutable desde acá
POST /api/informes/{id}/adenda             (Radiólogo asignado) — nuevo borrador encadenado a un informe Firmado
```

Un informe **firmado nunca se edita ni se borra** — es un registro clínico. Una corrección posterior se hace como adenda: un `Informe` nuevo con `InformeAnteriorId` apuntando al que corrige, visible en el historial pero sin reemplazar nada. Firmar el informe **original** (`InformeAnteriorId == null`) es lo único que pasa el `Estudio` a `Informado`; firmar una adenda no lo vuelve a mover.

Al firmar cualquier informe (original o adenda), se le manda un email al técnico que subió el estudio (`Estudio.SubidoPorId`) avisando que está listo. El envío es best-effort — si el SMTP falla, se loguea el error pero la firma ya quedó guardada, no se revierte.

> Admin de arranque en dev: `admin@teleradiologia.local` / `admin1234` (ver `Seed__AdminEmail`/`Seed__AdminPassword` en `docker-compose.yml`). Cambiar antes de cualquier despliegue real, junto con `Jwt__Key`.

### Visor DICOM

```
GET /api/estudios/{id}                         (cualquier autenticado) — metadata de un estudio puntual
GET /api/estudios/{id}/imagenes                 (cualquier autenticado) — lista de slices ordenadas; audita VioEstudio
GET /api/estudios/{id}/imagenes/{orthancId}     (cualquier autenticado) — proxy al render de Orthanc
```

El front nunca habla con Orthanc directo — todo pasa por este proxy autenticado con JWT (Orthanc tiene sus propias credenciales, separadas, que el front no conoce). `VioEstudio` se audita **una vez por apertura** (al pedir la lista de imágenes), no una vez por slice — abrir el visor cuenta como "vio el estudio", no cada frame que scrollea.

> Simplificación deliberada del MVP: usamos el render 8-bit de Orthanc (`/instances/{id}/rendered`), no una librería de render DICOM real (Cornerstone3D/OHIF). Alcanza para navegar slices y confirmar que el flujo end-to-end funciona, pero el windowing es fijo (el que trae el DICOM) y no hay zoom ni medición — para uso diagnóstico real hace falta migrar el visor a Cornerstone3D consumiendo WADO-RS.
>
> Usamos `/rendered` y **no** `/preview` a propósito: `/preview` normaliza por el min/max de los píxeles, lo que en estudios reales lava la imagen hasta dejarla casi blanca. `/rendered` aplica la ventana (`WindowCenter`/`WindowWidth`) y la LUT que vienen en el propio DICOM.

`<img>` no puede mandar el header `Authorization`, así que el front pide cada imagen con Axios (`responseType: 'blob'`) y arma un `URL.createObjectURL` — no se puede simplemente apuntar un `<img src>` al endpoint.

## Desarrollo local

### 1. Levantar Postgres, Orthanc, Mailpit y la API

```bash
docker compose up -d
```

- API: http://localhost:5080/api/health
- Orthanc (explorador web): http://localhost:8042 (usuario `admin` / clave `admin`)
- Mailpit (bandeja de entrada de desarrollo): http://localhost:8025 — ahí aparecen los emails de "informe listo"
- Postgres: `localhost:5432` (db `teleradiologia`, usuario `teleradiologia`, clave `teleradiologia`)

> Credenciales solo para desarrollo local — cambiar antes de cualquier despliegue real.

### 2. Levantar el frontend

```bash
cd frontend
npm install
npm run dev
```

Abre [http://localhost:5173](http://localhost:5173) — redirige a `/login`. Entrás con el Admin de arranque (`Seed:AdminEmail` / `Seed:AdminPassword` en `appsettings.json`), que ya tiene UI para crear usuarios (**Usuarios**) y para subir estudios (**Subir estudio**).

### 3. Datos de prueba: DICOM reales

El visor no se puede validar con archivos inventados — los DICOM reales traen *sequences*, ventanas de contraste y series de cientos de cortes que los sintéticos no tienen (de hecho, ambas cosas rompieron el pipeline la primera vez que se probó con datos reales).

La fuente más práctica es el **servidor demo público de Orthanc**, que expone datasets anonimizados por REST y permite bajar una serie entera como zip:

```bash
# Listar los estudios disponibles
curl -s "https://orthanc.uclouvain.be/demo/studies?expand" | jq '.[].PatientMainDicomTags.PatientName'

# Bajar una serie completa (BRAINIX, RM de cerebro, 100 cortes, ~5 MB)
curl -L -o brainix.zip "https://orthanc.uclouvain.be/demo/series/dc0216d2-a406a5ad-31ef7a78-113ae9d9-29939f9e/archive"

# PHENIX, CT de cráneo/senos paranasales, 361 cortes, ~60 MB
curl -L -o phenix.zip "https://orthanc.uclouvain.be/demo/series/7696013f-4c89c563-2b071693-5d1f97f6-f8ab232d/archive"
```

Descomprimir y subir los `.dcm` desde **Subir estudio** (se pueden seleccionar todos los cortes de la serie de una vez).

Otras fuentes libres: [Rubo Medical](https://www.rubomedical.com/dicom_files/) (archivos sueltos muy chicos, ideales para una prueba rápida) y [Medimodel](https://medimodel.com/sample-dicom-files/). Los datasets de OsiriX (BRAINIX, PHENIX, etc.) **ya no son de descarga libre** — pasaron a membresía paga; los mismos estudios siguen accesibles por el demo de Orthanc.

## Despliegue (Coolify)

`docker-compose.yml` es **solo para desarrollo local**. Para producción está `docker-compose.prod.yml`, pensado para [Coolify](https://coolify.io/) (PaaS self-hosted sobre Docker + Traefik).

### Qué cambia respecto de dev, y por qué

| | Dev (`docker-compose.yml`) | Prod (`docker-compose.prod.yml`) |
|---|---|---|
| Postgres / Orthanc | publican puertos al host | **sin `ports:`** — solo red interna |
| Frontend | dev server de Vite con proxy `/api` | contenedor nginx: sirve el bundle + proxea `/api` |
| Secretos | literales en el compose | variables de entorno de Coolify |
| Email | Mailpit | SMTP real |
| `ASPNETCORE_ENVIRONMENT` | `Development` | `Production` |

Lo más importante es lo primero de la tabla: en dev, `ports:` apunta a tu `localhost`; en una VPS es **internet abierto**. Orthanc trae su propio visor web en el 8042 — publicarlo deja navegar estudios de pacientes sin pasar por la API ni por la auditoría.

### El frontend en producción

En dev, quien resuelve `baseURL: '/api'` (ver `src/services/api.ts`) es el proxy de `vite.config.ts`. En producción no hay Vite, así que ese rol lo toma nginx (`frontend/nginx.conf`). Como el frontend y el API quedan en el mismo origen, **CORS deja de aplicar** y el API no necesita exponerse por separado.

Tres cosas de ese nginx que no son obvias:

- **`client_max_body_size 220M`** — el default de nginx es 1 MB y cortaría con un 413 cualquier subida de estudio, mucho antes de llegar al límite de 200 MB del `[RequestSizeLimit]` del controller.
- **MIME `application/wasm`** — los decodificadores DICOM de Cornerstone son WebAssembly; con el tipo equivocado, `instantiateStreaming` los rechaza y el visor no levanta.
- **`index.html` sin caché** — es quien apunta a los assets con hash nuevos después de cada deploy.

### Headers reenviados

`Program.cs` arranca con `UseForwardedHeaders`. El motivo concreto es la **auditoría**: detrás de dos proxies (Traefik → nginx), `HttpContext.Connection.RemoteIpAddress` es la IP del contenedor de nginx, así que sin esto todos los logins quedarían registrados con la misma IP interna en vez de la del usuario. Verificado: con `X-Forwarded-For: 203.0.113.45`, el `AuditLog` guarda esa IP y no la de la red de Docker.

El segundo motivo es preventivo. Con `ASPNETCORE_ENVIRONMENT=Production` se activa `UseHttpsRedirection`, que vería `scheme=http` y redirigiría a algo que le vuelve a llegar como HTTP — un loop de 307. **Hoy eso no pasa**: con `ASPNETCORE_URLS=http://+:8080` no hay puerto HTTPS que deducir, el middleware no hace nada y lo avisa al arrancar (`Failed to determine the https port for redirect`). Pero es un no-op por accidente: alcanza con que alguien defina `ASPNETCORE_HTTPS_PORTS` para que empiece a redirigir.

### Pasos en Coolify

1. **Servidor**: mínimo 2 vCPU / 4 GB RAM. Si dejás que Coolify compile en la VPS, el SDK de .NET pide bastante más; ver el punto 5.
2. **Recurso** → *Docker Compose* → apuntar al repo y a `docker-compose.prod.yml`.
3. **Variables de entorno**: copiar las de `.env.prod.example` y completarlas. Las marcadas con `:?` en el compose hacen fallar el arranque si faltan — es a propósito, mejor que levantar con una clave JWT vacía.
4. **Dominio**: asignarlo al servicio **`frontend`**, puerto 80. Es el único expuesto; los demás no llevan dominio.
5. **Build**: por defecto Coolify compila en la VPS. Funciona, pero compila el SDK de .NET 10 (>1 GB) y el bundle de Vite en la misma máquina que corre Postgres y Orthanc. La alternativa es `.github/workflows/deploy.yml`: construye en Actions, publica en GHCR y avisa a Coolify por webhook — el deploy en la VPS pasa a ser solo un `pull`. Requiere los secrets `COOLIFY_WEBHOOK_URL` y `COOLIFY_TOKEN`, y setear `IMAGE_PREFIX` / `IMAGE_TAG`.
6. **Backups**: el volumen `orthanc-data` crece rápido — un TC de 361 cortes ronda los 180 MB. Backupear los dos volúmenes, no solo Postgres.
7. **Primer login**: entrar con `SEED_ADMIN_EMAIL` / `SEED_ADMIN_PASSWORD` y cambiar la contraseña enseguida. El seed solo corre si la base está vacía.

### Limitaciones conocidas

- **SMTP en el puerto 587 no funciona.** `SmtpEmailSender` solo maneja TLS directo (`SslOnConnect`, puerto 465) o texto plano — no implementa STARTTLS, que es lo que usa el 587. Por eso `SMTP_PORT` viene en 465. Para usar 587 hay que cambiar `SecureSocketOptions` a `StartTls` en `SmtpEmailSender`.
- **Una sola instancia del API.** `Program.cs` corre migraciones y seed al arrancar; con dos réplicas, las dos migrarían a la vez.
- **Sin health checks de arranque en Orthanc.** El API depende de `service_started`, no de `service_healthy`: si Orthanc tarda, las primeras subidas pueden fallar.
- **Las claves de Data Protection viven dentro del contenedor** (`/root/.aspnet/DataProtection-Keys`) y se pierden en cada redeploy. Hoy no molesta porque el JWT se firma con `Jwt:Key`, que viene de configuración. Va a molestar al implementar reset de contraseña: los tokens emitidos antes del deploy dejarían de validar. Se arregla montando un volumen o persistiendo las claves en Postgres.
- **`Microsoft.OpenApi` 2.0.0 tiene una vulnerabilidad alta conocida** ([GHSA-v5pm-xwqc-g5wc](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc)) — el build la reporta como `NU1903`. Conviene actualizarla antes de exponer el servicio.

## Estructura del proyecto

```
backend/
└── src/
    ├── Teleradiologia.Domain/
    │   ├── Entities/               # Paciente, Estudio, Informe, AuditLog
    │   └── Enums/
    ├── Teleradiologia.Application/
    │   ├── Abstractions/           # Puertos: IIdentityService, IOrthancClient, IEstudioRepository, IEmailSender, ...
    │   ├── Auth/                   # LoginRequest/Response, IAuthService + AuthService
    │   ├── Usuarios/                # CrearUsuarioRequest, UsuarioResponse, IUsuarioService + UsuarioService
    │   ├── Estudios/                 # SubirEstudioRequest, EstudioResponse, IEstudioService + EstudioService
    │   ├── Informes/                   # CrearInformeRequest, InformeResponse, IInformeService + InformeService
    │   ├── Common/                       # UsuarioNombreCache + Exceptions/ (CredencialesInvalidasException, ...)
    │   └── DependencyInjection.cs          # AddApplication()
    ├── Teleradiologia.Infrastructure/
    │   ├── Identity/                # ApplicationUser (IdentityUser) + IdentityService (adaptador)
    │   ├── Persistence/              # AppDbContext (IdentityDbContext) + Configurations/ + Migrations/
    │   ├── Orthanc/                    # OrthancClient (HttpClient) — sube instancias, lee tags DICOM
    │   ├── Email/                        # EmailOptions + SmtpEmailSender (MailKit)
    │   ├── Repositories/                   # PacienteRepository, EstudioRepository, InformeRepository, AuditLogRepository
    │   ├── Security/                         # JwtOptions + JwtTokenService
    │   ├── Seed/                              # AdminSeeder
    │   └── DependencyInjection.cs               # AddInfrastructure() — DbContext, Identity, JWT, Orthanc, Email, repos
    └── Teleradiologia.Api/
        ├── Controllers/                 # Endpoints — finos, sin lógica de negocio
        ├── ExceptionHandling/            # AppExceptionHandler (excepciones de Application → HTTP)
        └── Program.cs                     # Composition root: DI, auth middleware, migración+seed al arrancar

frontend/
└── src/
    ├── layouts/AppLayout.vue   # Shell: header con nav horizontal (logo, menú, usuario, salir)
    ├── views/                    # LoginView, WorklistView, EstudioDetalleView (el visor)
    ├── stores/auth.ts              # Pinia: token + usuario, persistido en localStorage
    ├── router/                      # Rutas + guard de autenticación
    ├── services/api.ts                # Axios: interceptor de Bearer token + logout en 401
    └── types/                          # Interfaces TS que espejan los DTOs de la Api

frontend/Dockerfile        # Build con Node → nginx sirve el bundle y proxea /api (solo prod)
frontend/nginx.conf        # SPA fallback, límite de subida, MIME de los .wasm de Cornerstone

docker-compose.yml         # Postgres + Orthanc + Mailpit + API para desarrollo local
docker-compose.prod.yml    # Stack de producción para Coolify (sin puertos expuestos, sin secretos)
.env.prod.example          # Plantilla de las variables que espera el compose de producción
.github/workflows/deploy.yml # Build en Actions → GHCR → webhook de deploy a Coolify
```

## Notas de seguridad (aplican desde el MVP, no son opcionales)

- Los estudios e informes son datos de salud sensibles — control de acceso por rol y por paciente desde el día uno.
- Toda vista de un estudio debe quedar en un log de auditoría (quién, cuándo, qué estudio).
- Cifrado en tránsito (TLS) obligatorio antes de cualquier ambiente que no sea `localhost`.
- `Jwt:Key` viaja en `appsettings.json`/`docker-compose.yml` solo porque es un valor de desarrollo — en cualquier despliegue real va por user-secrets/variables de entorno del proveedor, nunca en el repo.
