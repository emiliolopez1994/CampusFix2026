# Inventario funcional conservado

| Módulo | Funcionalidad incluida |
|---|---|
| Acceso | Login, registro de solicitantes, carrusel de cuatro imágenes, recordar correo, ayuda de recuperación |
| Sesiones | JWT firmado, expiración, cierre de sesión, bloqueo por intentos, revocación por cambio de contraseña o permisos |
| Vista general | Resumen operativo, pendientes, prioridades, atención vencida y accesos a incidencias |
| Tablero de estados | Tablero, lista, historial resuelto, filtros, orden, paginación, arrastre al siguiente estado y exportación CSV |
| Tickets | Creación, edición, asignación, estados, respuestas, notas internas, adjuntos, historial, solución y archivo lógico |
| Valoración | Calificación del solicitante vinculada al técnico responsable al cierre |
| Base de conocimiento | Borradores desde soluciones, revisión, publicación, visibilidad, versiones, búsqueda, utilidad y reutilización |
| Analítica | Carga real, respuesta, resolución, cumplimiento de metas, valoraciones y conocimiento |
| Equipo | Técnicos, carga, resultados y valoraciones |
| Áreas | Distribución y estado de incidencias por área |
| Informes | Informes CSV a partir de los datos visibles para el usuario y filtros disponibles |
| Usuarios | Altas, roles permitidos, activación, desactivación, áreas y restablecimiento de contraseña |
| Roles y permisos | Permisos por rol, excepciones individuales, alcance de tickets, herencia y auditoría |
| Configuración | Organización, áreas, sedes, ubicaciones, equipos, servicios, categorías, campos dinámicos, calendarios y metas |
| Notificaciones | Bandeja, contador, lectura, eventos de tickets y aviso de metas vencidas |
| Correo | Configuración Resend cifrada, prueba, activación y procesamiento de cola con reintentos |
| Mi cuenta | Cambio de contraseña y cambio obligatorio después de un restablecimiento |
| Diseño | Tema claro/oscuro, transiciones, gráficas SVG, diálogos adaptables y carrusel existente |

Las reglas del servidor distinguen Solicitante, Técnico, Administrador y Superadministrador. Los permisos efectivos combinan rol y excepciones del usuario; la cuenta propietaria conserva su protección. No se añadió una segunda versión reducida del panel.

La API conserva las rutas que usa Angular. Se eliminó del paquete la infraestructura de React/Next/Vinext/Cloudflare y se sustituyó el almacenamiento del backend por EF Core/PostgreSQL. El envío de correo mantiene su proveedor externo opcional; no es un reemplazo del backend C#.
