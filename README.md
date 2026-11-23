# HospitalSystem — Gestión Hospitalaria (.NET 10 / C# 14 / WPF-UI / MySQL)

Aplicación de escritorio para Windows desarrollada con **.NET 10**, **C# 14**, **WPF-UI** y **MySQL (`MySqlConnector`)**, diseñada como entorno práctico para probar operaciones relacionales, funciones almacenadas y transacciones en **MySQL** bajo una arquitectura limpia por capas.

---

## Características Principales

- **Panel de Indicadores en Tiempo Real**:
  - **Camas disponibles** vs. capacidad total y ocupación porcentual por sala.
  - **Cantidad de salas** activas en el hospital (Urgencias, Observación, Medicina Interna, Pediatría y UCI).
  - **Médicos del día** asignados a la guardia actual.
  - **Probador e inicializador MySQL integrado**: permite probar la conexión en vivo o ejecutar `schema.sql` + `seed.sql` directamente desde la interfaz gráfica.
- **Menú Pacientes**:
  - Registro obligatorio con **Cédula**, **Nombres** y **Síntomas**, además de sexo, convenio, asignación de médico del día y asignación opcional de cama libre.
  - Opción integrada para generar **salida inmediata a laboratorio** durante el ingreso si la condición clínica lo requiere, o derivar pacientes ya ingresados y otorgar **Alta Médica** (liberando su cama automáticamente).
- **Menú Médicos**:
  - Visualización filtrada de **Médicos del Día** y plantilla médica completa.
  - Botón rápido para incorporar o retirar médicos de la guardia del día y formulario de alta de nuevos especialistas.
  - **Plantilla inicial propuesta**:
    - *Dra. Valentina Rojas Méndez* — Medicina de Urgencias (Guardia 24h)
    - *Dr. Alejandro Silva Paredes* — Medicina Interna (Mañana)
    - *Dra. Camila Herrera Soto* — Cardiología Clínica (Mañana)
    - *Dr. Sebastián Morales Vega* — Infectología y Diagnóstico (Tarde)
    - *Dra. Lucía Fernández Castro* — Pediatría (Tarde)
    - *Dr. Mateo Navarro Guzmán* — Medicina Intensiva / UCI (Noche)
    - Más especialistas fuera de turno (*Traumatología* y *Neurología*) para probar el cambio de guardia.
- **Salidas a Laboratorios**:
  - Derivación de pacientes hacia laboratorios internos (Hematología, Imagenología, Microbiología) o externos por convenio.
  - Recepción de resultados clínicos y retorno automático del estado de atención del paciente.

---

## Arquitectura de la Solución

La solución (`HospitalSystem.slnx`) sigue una estructura **Clean Architecture** pragmática:

1. **`HospitalSystem.Core`**: Entidades del dominio (`Paciente`, `Medico`, `Sala`, `Cama`, `Atencion`, `Laboratorio`, `SalidaLaboratorio`, etc.), enumeraciones clínicas, excepciones de dominio y extensiones.
2. **`HospitalSystem.Application`**: DTOs inmutables (`record`), contrato `IHospitalRepository` y casos de uso en `HospitalServices`.
3. **`HospitalSystem.Infrastructure`**: Acceso a datos con `MySqlConnector` (`HospitalRepository`), consumo de vistas, funciones y procedimientos almacenados de MySQL, ejecutor de scripts SQL y respaldo desconectado en memoria (`HospitalDataStore`).
4. **`HospitalSystem.Desktop`**: Interfaz gráfica WPF con patrón **MVVM** (`CommunityToolkit.Mvvm`) y tema moderno Fluent oscuro mediante **WPF-UI**, evitando diccionarios XAML extensos de estilos manuales.
5. **`HospitalSystem.Console`**: Utilidad ligera por línea de comandos para verificar rápidamente la conexión a MySQL y los indicadores del hospital.

---

## Uso de C# 14 y WPF-UI

- **C# 14 (`<LangVersion>14.0</LangVersion>`)**:
  - Palabra clave **`field`** en descriptores de acceso (`get`/`set`) tanto en entidades (`Paciente`, `Medico`) como en propiedades observables de `MainViewModel`, eliminando campos privados redundantes (`_campo`).
  - **Bloques `extension(...)`** en `HospitalDomainExtensions` para definir propiedades de extensión sobre `Sala`, `Medico` y `EstadoSalidaLaboratorio`.
  - **Asignación condicional nula (`?.=`)** al actualizar estados de camas, guardias médicas y atenciones clínicas.
- **WPF-UI (`Lepoco.WpfUi` 4.3.0) + Identidad Visual Hospitalaria**:
  - Tema clínico luminoso (`Theme="Light"`) con paleta médica en [HospitalStyles.xaml](./HospitalSystem.Desktop/Themes/HospitalStyles.xaml) (blanco sanitario, celeste clínico `#0284C7`, verde esmeralda `#059669` y ámbar diagnóstico `#D97706`).
  - **Navbar superior hospitalario** con pestañas interactivas, subrayado de sección activa, píldoras KPI en tiempo real e indicador pulsante del estado de MySQL.
  - **Feedback visual enriquecido y animaciones**: banner clínico animado por severidad (*Éxito*, *Información*, *Advertencia*, *Error*), barra de carga activa, botones con cambio de color y escala suave en **Hover**, tarjetas KPI con elevación animada y transiciones *fade + slide* al cambiar de vista.

---

## Cómo Ejecutar el Proyecto

### 1. Compilar toda la solución
```powershell
dotnet build HospitalSystem.slnx
```

### 2. Conectar MySQL (opcional al primer arranque)
Revisa todos los pasos detallados y la documentación de funciones SQL en [`CONEXION_MYSQL.md`](./CONEXION_MYSQL.md).

### 3. Iniciar la aplicación de escritorio (WPF)
```powershell
dotnet run --project HospitalSystem.Desktop
```

---

## Documentación Adicional

- [`CONEXION_MYSQL.md`](./CONEXION_MYSQL.md): Guía paso a paso para conectar MySQL, diseño entidad-relación y documentación de funciones y procedimientos almacenados.
- [`BITACORA_DESARROLLO.md`](./BITACORA_DESARROLLO.md): Bitácora técnica del proceso de construcción, decisiones de diseño y pruebas realizadas.
