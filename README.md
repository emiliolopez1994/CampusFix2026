CampusFix
Sistema web de gestión y seguimiento de incidencias institucionales
CampusFix es una aplicación web orientada a centralizar el registro, la clasificación, la asignación y el seguimiento de incidencias dentro de una institución. Permite coordinar el trabajo entre solicitantes, técnicos y administradores, mantener la trazabilidad de cada solicitud y consultar información para apoyar la gestión del servicio.
El proyecto se desarrolla como una solución académica de gestión institucional, con una interfaz web, una API de negocio, persistencia en PostgreSQL y un servicio independiente para generar informes imprimibles.
Objetivo
Facilitar la administración de solicitudes institucionales mediante un sistema que organice las incidencias, diferencie responsabilidades por rol, permita supervisar su evolución y conserve antecedentes de atención y soluciones documentadas.
Funcionalidades principales
- Gestión de incidencias: registro de solicitudes, clasificación, asignación de responsables, seguimiento de estados e historial de atención.
- Tablero de estados: visualización de las incidencias según su situación de atención.
- Usuarios, roles y permisos: administración de cuentas y autorización de operaciones según las responsabilidades de cada usuario.
- Configuración institucional: gestión de áreas, sedes, ubicaciones, equipos técnicos, servicios, categorías, calendarios y metas de atención.
- Notificaciones internas: avisos dentro de la plataforma sobre actividades relacionadas con las solicitudes.
- Analítica: indicadores y gráficos construidos a partir de la información registrada en el sistema.
- Informes: consultas y exportaciones CSV, complementadas con informes imprimibles generados por un servicio independiente.
- Base de conocimiento: consulta de artículos y soluciones documentadas como referencia para futuras atenciones.
- Equipo y desempeño: información de seguimiento del trabajo técnico.
Roles del sistema
CampusFix contempla cuatro roles principales:
Rol	Responsabilidad general
Superadministrador	Administración global del sistema y su configuración.
Administrador	Supervisión y gestión de incidencias dentro de su ámbito autorizado.
Técnico	Atención y actualización de las incidencias asignadas.
Solicitante	Registro y seguimiento de sus solicitudes.


Las operaciones disponibles dependen de los permisos y del ámbito configurado para cada cuenta.
Tecnologías utilizadas
Componente	Tecnología
Interfaz web	Angular 22 y TypeScript
API y lógica de negocio	ASP.NET Core 10 / C#
Persistencia	PostgreSQL 18
Acceso a datos	Entity Framework Core y Npgsql
Autenticación y autorización	ASP.NET Core Identity y JWT
Control de versiones	Git y GitHub


Arquitectura
El frontend Angular consume los endpoints de la API ASP.NET Core, donde se aplican las reglas de negocio y las verificaciones de autorización. PostgreSQL almacena la información de usuarios y los registros funcionales de la aplicación.
La solución incorpora además CampusFix.Reports, un servicio independiente para la generación de informes imprimibles. Esta separación complementa las exportaciones CSV de la aplicación principal.
Las notificaciones dentro de CampusFix son independientes de la configuración opcional de envío de correo electrónico. La integración de correo requiere un remitente autorizado y una configuración válida del proveedor correspondiente.
Organización del repositorio
CampusFix/
├── backend/CampusFix.Api/          # API ASP.NET Core
├── frontend/campusfix-web/        # Aplicación Angular
├── database/                      # Recursos de base de datos
├── services/CampusFix.Reports/    # Servicio de informes imprimibles
├── scripts/                       # Preparación y ejecución local
├── tests/CampusFix.Checks/        # Comprobaciones del sistema
├── docs/                          # Documentación técnica y pruebas
├── Dockerfile                     # Configuración de contenedor
└── README.md
Ejecución local
Requisitos
- .NET SDK 10
- Node.js y npm compatibles con el proyecto Angular
- PostgreSQL 18
- Git
Preparación
1. Clonar el repositorio y abrir la carpeta del proyecto.
2. Configurar una base de datos PostgreSQL local y la cadena de conexión de la API mediante un mecanismo seguro de configuración.
3. Preparar las dependencias y la base de datos conforme a los scripts y documentos incluidos en el repositorio.
4. Iniciar la API y el frontend desde sus respectivos proyectos.
5. Si se requieren informes imprimibles, iniciar también el servicio de informes mediante scripts/Iniciar-InformesLocales.ps1.
Comandos de desarrollo de referencia:
API:
dotnet run --project backend/CampusFix.Api
Frontend:
cd frontend/campusfix-web
npm ci
npm start
En la configuración local de desarrollo utilizada para el proyecto, el frontend se abre en http://localhost:4200 y la API en http://localhost:5137. Estos puertos pueden variar según el entorno y los perfiles de ejecución.
Seguridad: no publicar contraseñas, claves JWT, cadenas de conexión con credenciales ni claves de servicios externos. La configuración de cada instalación debe mantenerse fuera del control de versiones.

Flujo general de atención
1. El solicitante registra una incidencia con su información y clasificación.
2. El administrador revisa la solicitud y asigna al responsable autorizado.
3. El técnico realiza la atención y documenta el avance o la solución.
4. Los usuarios autorizados consultan el estado y el historial de la incidencia.
5. La información registrada alimenta el tablero, la analítica y los informes.
Calidad y trazabilidad
El proyecto incluye controles de acceso por roles y permisos, historial de incidencias, documentación técnica y comprobaciones de negocio en tests/CampusFix.Checks. La carpeta docs contiene material complementario de pruebas y funcionamiento.
Alcance
CampusFix está diseñado como un proyecto académico de gestión de incidencias institucionales. Las funcionalidades y los servicios externos disponibles dependen de la configuración del entorno donde se ejecuta la aplicación.
CampusFix — Gestión institucional de incidencias con trazabilidad, seguimiento y conocimiento técnico.