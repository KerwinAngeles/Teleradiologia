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
| Backend | ASP.NET Core Web API (.NET 10) | — |
| DICOM / mini-PACS | [Orthanc](https://www.orthanc-server.com/) | Recibe, indexa y expone DICOM vía REST/DICOMweb — no reinventamos el manejo de DICOM. |
| Frontend | Vue 3 + TypeScript + Tailwind CSS v4 | Consistente con el resto de nuestros proyectos. |
| Visor de imágenes | Pendiente: OHIF Viewer o Cornerstone.js embebido | Aún no integrado — es la próxima feature. |
| Base de datos | PostgreSQL (EF Core / Npgsql) | — |
| Infra local | Docker Compose | Postgres + Orthanc + API con un solo comando. |

## Estado actual: esqueleto

Este repo por ahora **no tiene features clínicas** — es el esqueleto validado end-to-end:

- ✅ API .NET arriba, conectada a Postgres (`GET /api/health`)
- ✅ Orthanc arriba y accesible (mini-PACS, protegido con usuario/clave)
- ✅ Frontend Vue arriba, consumiendo la API a través del proxy de Vite
- ⬜ Subida de estudios DICOM
- ⬜ Worklist del radiólogo
- ⬜ Visor DICOM en el navegador
- ⬜ Redacción/firma de informe
- ⬜ Notificación al hospital de origen
- ⬜ Auditoría (quién vio qué estudio y cuándo)
- ⬜ Autenticación y roles (técnico / radiólogo / admin)

## Desarrollo local

### 1. Levantar Postgres, Orthanc y la API

```bash
docker compose up -d
```

- API: http://localhost:5080/api/health
- Orthanc (explorador web): http://localhost:8042 (usuario `admin` / clave `admin`)
- Postgres: `localhost:5432` (db `teleradiologia`, usuario `teleradiologia`, clave `teleradiologia`)

> Credenciales solo para desarrollo local — cambiar antes de cualquier despliegue real.

### 2. Levantar el frontend

```bash
cd frontend
npm install
npm run dev
```

Abre [http://localhost:5173](http://localhost:5173) — debería mostrar `API: ok · DB: connected`.

## Estructura del proyecto

```
backend/
└── src/Teleradiologia.Api/
    ├── Controllers/     # Endpoints de la API
    ├── Data/             # AppDbContext (EF Core)
    └── Program.cs        # Configuración: DB, CORS, etc.

frontend/
└── src/
    ├── views/            # Vistas (páginas)
    ├── router/            # Configuración de rutas
    └── services/          # Cliente Axios hacia la API

docker-compose.yml         # Postgres + Orthanc + API para desarrollo local
```

## Notas de seguridad (aplican desde el MVP, no son opcionales)

- Los estudios e informes son datos de salud sensibles — control de acceso por rol y por paciente desde el día uno.
- Toda vista de un estudio debe quedar en un log de auditoría (quién, cuándo, qué estudio).
- Cifrado en tránsito (TLS) obligatorio antes de cualquier ambiente que no sea `localhost`.
