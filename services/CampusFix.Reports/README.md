# CampusFix.Reports

Microservicio adicional ASP.NET Core (.NET 10), independiente del backend principal. Genera informes HTML imprimibles (el navegador permite guardarlos como PDF). No sustituye el CSV ni interviene en tickets, notificaciones o conocimiento. No necesita base de datos: procesa en memoria y no conserva solicitudes ni documentos.

## Uso local

Ejecutar `pwsh -File scripts/Iniciar-InformesLocales.ps1 -Build` desde la raíz. La primera ejecución crea una clave exclusiva en los secretos locales de desarrollo de la API (fuera de Git); reiniciar la API compilada con los cambios y entorno Development para que la lea. Iniciar Angular como habitualmente. En Centro de informes, seleccionar filtros y pulsar «Descargar informe imprimible». Abrir el HTML y usar Imprimir / Guardar como PDF.

El proceso escucha solo en `127.0.0.1:5148`. `GET /health` indica disponibilidad. `POST /v1/reports/printable` requiere `X-Reports-Key` y recibe `{ "tickets": [{ "id":"1", "subject":"Consulta", "area":"Biblioteca", "service":"Préstamos", "assignee":"Responsable", "state":1, "priority":2 }] }`. Máximo 5000 registros y 2 MB. La clave no se entrega a Angular. No hay CORS ni acceso directo a PostgreSQL.

Angular solicita `POST /api/informes/imprimible` con los IDs seleccionados. La API valida sesión vigente, permiso de analítica, alcance de cada ticket y eliminación lógica; únicamente envía las columnas del informe. El servicio escapa HTML y no genera scripts. La API tiene un tiempo máximo de espera de 8 segundos. Si el proceso está detenido, la nueva descarga devuelve 503 y los módulos existentes siguen independientes.

Arquitectura resultante: monolito modular con un microservicio complementario, no una migración completa a microservicios. Puede compilarse y reiniciarse por separado. La configuración de esta entrega es exclusivamente local; no se ha añadido despliegue ni migración SQL.
