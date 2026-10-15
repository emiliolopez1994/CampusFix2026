# CampusFix — Angular + ASP.NET Core + PostgreSQL

Empieza por **INSTALACION.md**. El frontend compilado está incluido, de modo que la primera ejecución no requiere reconstruir Angular.

- `frontend/campusfix-web`: Angular, Router, HttpClient, formularios y diseño del sistema existente.
- `backend/CampusFix.Api`: Web API C#, Identity, JWT, permisos y lógica del sistema; `wwwroot` sirve Angular.
- `database`: migración SQL PostgreSQL, datos SQL y respaldo JSON importable.
- `scripts`: configuración, compilación, importación y arranque en Windows.
- `tests/CampusFix.Checks`: comprobaciones de negocio y modo opcional PostgreSQL.
- `docs`: registros de compilación/pruebas, inventario y ejemplos de API.
- `services/CampusFix.Reports`: microservicio adicional de informes imprimibles; arranque local con `scripts/Iniciar-InformesLocales.ps1`. Conserva el backend modular y los informes CSV existentes. Consulta su README para configuración y contrato.
- `services/CampusFix.Continuity`: continuidad académica con aulas, actividades, impacto de incidencias y alternativas aprobadas. PostgreSQL local independiente y arranque con `scripts/Iniciar-ContinuidadLocal.ps1`; preparación y ejemplos en su README.
- `VERIFICACION.md`: resultados comprobados y límites pendientes.

Se conservan login con carrusel, tema claro/oscuro, vistas adaptables, panel, tablero, historial, conocimiento, analítica, equipo, áreas, informes CSV, usuarios, roles, permisos, configuración, notificaciones y cuenta.

## Persistencia

Identity usa sus tablas relacionales de usuarios, roles y membresías. Los registros funcionales conservan su estructura original en `Documentos`, con clave compuesta `(Kind, Id)`, contenido PostgreSQL `jsonb` y versión de concurrencia. Esto preserva campos dinámicos, historial, versiones de artículos y referencias existentes. `Secuencias` asigna códigos de ticket de forma atómica. `Archivos` guarda los adjuntos como `bytea`.

La base utiliza PostgreSQL mediante Entity Framework Core y Npgsql; no utiliza SQLite. La persistencia funcional mantiene un modelo documental JSONB: no se presenta como una conversión a tablas normalizadas independientes por cada módulo. Una normalización adicional exigiría otra migración y no es necesaria para conservar el contrato actual.

Las operaciones funcionales de la API agrupan cambios relacionados en transacciones. Los permisos se verifican en el servidor, y las modificaciones de contraseña/permisos invalidan sesiones. Las contraseñas existentes son hashes Identity V3 compatibles, no contraseñas recuperadas en texto plano.

Los gráficos se alimentan de tickets, respuestas, soluciones y valoraciones guardados. La actualización periódica existente es por consultas HTTP; no se ha sustituido por un sistema de eventos push.

El proyecto de esta carpeta es independiente del sitio publicado en ChatGPT Sites. No contiene herramientas ni credenciales para modificar ese sitio.
