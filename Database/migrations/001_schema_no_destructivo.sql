-- ============================================================================
-- Migración 001 (No Destructiva): Creación segura de tablas base sin DROP TABLE
-- Uso recomendado para evolución futura del esquema conservando registros previos.
-- ============================================================================

CREATE DATABASE IF NOT EXISTS hospital_system_db
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE hospital_system_db;

CREATE TABLE IF NOT EXISTS hospitales (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(120) NOT NULL,
    codigo_habilitacion VARCHAR(30) NOT NULL UNIQUE,
    direccion VARCHAR(200) NOT NULL,
    telefono VARCHAR(30) NOT NULL
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS convenios (
    id INT AUTO_INCREMENT PRIMARY KEY,
    codigo VARCHAR(20) NOT NULL UNIQUE,
    nombre VARCHAR(100) NOT NULL,
    porcentaje_cobertura DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    activo TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS salas (
    id INT AUTO_INCREMENT PRIMARY KEY,
    hospital_id INT NOT NULL DEFAULT 1,
    codigo VARCHAR(20) NOT NULL UNIQUE,
    nombre VARCHAR(100) NOT NULL,
    piso INT NOT NULL DEFAULT 1,
    activa TINYINT(1) NOT NULL DEFAULT 1,
    CONSTRAINT fk_salas_hospital FOREIGN KEY (hospital_id) REFERENCES hospitales(id)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS camas (
    id INT AUTO_INCREMENT PRIMARY KEY,
    sala_id INT NOT NULL,
    codigo VARCHAR(20) NOT NULL UNIQUE,
    ocupada TINYINT(1) NOT NULL DEFAULT 0,
    observacion VARCHAR(150) NULL,
    CONSTRAINT fk_camas_sala FOREIGN KEY (sala_id) REFERENCES salas(id),
    INDEX idx_camas_disponibilidad (ocupada, sala_id)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS medicos (
    id INT AUTO_INCREMENT PRIMARY KEY,
    cedula_profesional VARCHAR(30) NOT NULL UNIQUE,
    nombres VARCHAR(120) NOT NULL,
    especialidad VARCHAR(80) NOT NULL,
    turno ENUM('Manana', 'Tarde', 'Noche', 'Guardia24h') NOT NULL DEFAULT 'Manana',
    consultorio VARCHAR(40) NOT NULL,
    en_guardia_hoy TINYINT(1) NOT NULL DEFAULT 1,
    activo TINYINT(1) NOT NULL DEFAULT 1,
    INDEX idx_medicos_dia (activo, en_guardia_hoy)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS pacientes (
    id INT AUTO_INCREMENT PRIMARY KEY,
    cedula VARCHAR(25) NOT NULL UNIQUE,
    nombres VARCHAR(140) NOT NULL,
    sexo ENUM('Masculino', 'Femenino', 'Otro') NOT NULL DEFAULT 'Otro',
    sintomas_actuales VARCHAR(500) NOT NULL,
    convenio_id INT NULL,
    fecha_registro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_pacientes_convenio FOREIGN KEY (convenio_id) REFERENCES convenios(id),
    INDEX idx_pacientes_cedula (cedula)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS diagnosticos (
    id INT AUTO_INCREMENT PRIMARY KEY,
    codigo_cie10 VARCHAR(15) NOT NULL UNIQUE,
    descripcion VARCHAR(200) NOT NULL
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS atenciones (
    id INT AUTO_INCREMENT PRIMARY KEY,
    paciente_id INT NOT NULL,
    medico_id INT NOT NULL,
    cama_id INT NULL,
    sintomas VARCHAR(500) NOT NULL,
    notas_medicas VARCHAR(500) NULL,
    requiere_laboratorio TINYINT(1) NOT NULL DEFAULT 0,
    estado ENUM('EnAtencion', 'EnLaboratorio', 'AltaMedica') NOT NULL DEFAULT 'EnAtencion',
    fecha_ingreso DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    fecha_egreso DATETIME NULL,
    CONSTRAINT fk_atenciones_paciente FOREIGN KEY (paciente_id) REFERENCES pacientes(id),
    CONSTRAINT fk_atenciones_medico FOREIGN KEY (medico_id) REFERENCES medicos(id),
    CONSTRAINT fk_atenciones_cama FOREIGN KEY (cama_id) REFERENCES camas(id),
    INDEX idx_atenciones_estado (estado)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS diagnosticos_paciente (
    id INT AUTO_INCREMENT PRIMARY KEY,
    atencion_id INT NOT NULL,
    paciente_id INT NOT NULL,
    diagnostico_id INT NOT NULL,
    confirmado TINYINT(1) NOT NULL DEFAULT 0,
    fecha_asignacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_diagpac_atencion FOREIGN KEY (atencion_id) REFERENCES atenciones(id),
    CONSTRAINT fk_diagpac_paciente FOREIGN KEY (paciente_id) REFERENCES pacientes(id),
    CONSTRAINT fk_diagpac_diagnostico FOREIGN KEY (diagnostico_id) REFERENCES diagnosticos(id)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS laboratorios (
    id INT AUTO_INCREMENT PRIMARY KEY,
    codigo VARCHAR(20) NOT NULL UNIQUE,
    nombre VARCHAR(120) NOT NULL,
    tipo ENUM('Interno', 'ExternoConvenio') NOT NULL DEFAULT 'Interno',
    ubicacion VARCHAR(150) NOT NULL,
    especialidad_analisis VARCHAR(120) NOT NULL,
    convenio_id INT NULL,
    activo TINYINT(1) NOT NULL DEFAULT 1,
    CONSTRAINT fk_laboratorios_convenio FOREIGN KEY (convenio_id) REFERENCES convenios(id)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS salidas_laboratorio (
    id INT AUTO_INCREMENT PRIMARY KEY,
    atencion_id INT NULL,
    paciente_id INT NOT NULL,
    medico_id INT NOT NULL,
    laboratorio_id INT NOT NULL,
    examen_solicitado VARCHAR(180) NOT NULL,
    motivo_clinico VARCHAR(350) NOT NULL,
    prioridad ENUM('Rutina', 'Urgente', 'Critica') NOT NULL DEFAULT 'Rutina',
    estado ENUM('PendienteSalida', 'EnLaboratorio', 'ResultadoRecibido') NOT NULL DEFAULT 'PendienteSalida',
    resultado_resumen VARCHAR(500) NULL,
    fecha_salida DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    fecha_retorno DATETIME NULL,
    CONSTRAINT fk_salidalab_atencion FOREIGN KEY (atencion_id) REFERENCES atenciones(id),
    CONSTRAINT fk_salidalab_paciente FOREIGN KEY (paciente_id) REFERENCES pacientes(id),
    CONSTRAINT fk_salidalab_medico FOREIGN KEY (medico_id) REFERENCES medicos(id),
    CONSTRAINT fk_salidalab_laboratorio FOREIGN KEY (laboratorio_id) REFERENCES laboratorios(id),
    INDEX idx_salidalab_estado (estado)
) ENGINE=InnoDB;
