-- ============================================================================
-- HospitalSystem - Esquema Relacional MySQL 8.0+
-- Propósito: Gestión hospitalaria de pacientes, médicos del día, salas, camas
--            y salidas a laboratorios clínicos.
-- ============================================================================

CREATE DATABASE IF NOT EXISTS hospital_system_db
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE hospital_system_db;

SET FOREIGN_KEY_CHECKS = 0;
DROP VIEW IF EXISTS vw_indicadores_hospital;
DROP PROCEDURE IF EXISTS sp_dar_alta_atencion;
DROP PROCEDURE IF EXISTS sp_registrar_ingreso_paciente;
DROP FUNCTION IF EXISTS fn_paciente_tiene_salida_activa;
DROP FUNCTION IF EXISTS fn_ocupacion_sala_pct;
DROP FUNCTION IF EXISTS fn_medicos_del_dia;
DROP FUNCTION IF EXISTS fn_total_salas_activas;
DROP FUNCTION IF EXISTS fn_camas_disponibles;

DROP TABLE IF EXISTS salidas_laboratorio;
DROP TABLE IF EXISTS diagnosticos_paciente;
DROP TABLE IF EXISTS atenciones;
DROP TABLE IF EXISTS laboratorios;
DROP TABLE IF EXISTS diagnosticos;
DROP TABLE IF EXISTS pacientes;
DROP TABLE IF EXISTS medicos;
DROP TABLE IF EXISTS camas;
DROP TABLE IF EXISTS salas;
DROP TABLE IF EXISTS convenios;
DROP TABLE IF EXISTS hospitales;
SET FOREIGN_KEY_CHECKS = 1;

-- 1. Hospital principal
CREATE TABLE hospitales (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(120) NOT NULL,
    codigo_habilitacion VARCHAR(30) NOT NULL UNIQUE,
    direccion VARCHAR(200) NOT NULL,
    telefono VARCHAR(30) NOT NULL
) ENGINE=InnoDB;

-- 2. Convenios médicos y aseguradoras
CREATE TABLE convenios (
    id INT AUTO_INCREMENT PRIMARY KEY,
    codigo VARCHAR(20) NOT NULL UNIQUE,
    nombre VARCHAR(100) NOT NULL,
    porcentaje_cobertura DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    activo TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB;

-- 3. Salas hospitalarias (Urgencias, Medicina Interna, UCI, etc.)
CREATE TABLE salas (
    id INT AUTO_INCREMENT PRIMARY KEY,
    hospital_id INT NOT NULL DEFAULT 1,
    codigo VARCHAR(20) NOT NULL UNIQUE,
    nombre VARCHAR(100) NOT NULL,
    piso INT NOT NULL DEFAULT 1,
    activa TINYINT(1) NOT NULL DEFAULT 1,
    CONSTRAINT fk_salas_hospital FOREIGN KEY (hospital_id) REFERENCES hospitales(id)
) ENGINE=InnoDB;

-- 4. Camas asignadas a cada sala
CREATE TABLE camas (
    id INT AUTO_INCREMENT PRIMARY KEY,
    sala_id INT NOT NULL,
    codigo VARCHAR(20) NOT NULL UNIQUE,
    ocupada TINYINT(1) NOT NULL DEFAULT 0,
    observacion VARCHAR(150) NULL,
    CONSTRAINT fk_camas_sala FOREIGN KEY (sala_id) REFERENCES salas(id),
    INDEX idx_camas_disponibilidad (ocupada, sala_id)
) ENGINE=InnoDB;

-- 5. Plantilla de médicos (incluye indicador de médicos de guardia del día)
CREATE TABLE medicos (
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

-- 6. Pacientes (requiere obligatoriamente Cédula, Nombres y Síntomas)
CREATE TABLE pacientes (
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

-- 7. Catálogo CIE-10 de diagnósticos clínicos
CREATE TABLE diagnosticos (
    id INT AUTO_INCREMENT PRIMARY KEY,
    codigo_cie10 VARCHAR(15) NOT NULL UNIQUE,
    descripcion VARCHAR(200) NOT NULL
) ENGINE=InnoDB;

-- 8. Atenciones clínicas / ingresos hospitalarios
CREATE TABLE atenciones (
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

-- 9. Diagnósticos asociados a un paciente durante su atención
CREATE TABLE diagnosticos_paciente (
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

-- 10. Laboratorios internos y externos por convenio
CREATE TABLE laboratorios (
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

-- 11. Salidas de pacientes a laboratorios cuando el caso clínico lo requiere
CREATE TABLE salidas_laboratorio (
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

-- ============================================================================
-- FUNCIONES ALMACENADAS Y PROCEDIMIENTOS MYSQL
-- ============================================================================

DELIMITER $$

-- Función 1: Cuenta total de camas libres en salas operativas
CREATE FUNCTION fn_camas_disponibles()
RETURNS INT
READS SQL DATA
BEGIN
    DECLARE v_disponibles INT DEFAULT 0;
    SELECT COUNT(*)
      INTO v_disponibles
      FROM camas c
      INNER JOIN salas s ON s.id = c.sala_id
     WHERE c.ocupada = 0
       AND s.activa = 1;
    RETURN v_disponibles;
END$$

-- Función 2: Cuenta total de salas activas en el hospital
CREATE FUNCTION fn_total_salas_activas()
RETURNS INT
READS SQL DATA
BEGIN
    DECLARE v_salas INT DEFAULT 0;
    SELECT COUNT(*)
      INTO v_salas
      FROM salas
     WHERE activa = 1;
    RETURN v_salas;
END$$

-- Función 3: Cuenta cuántos médicos están asignados como médicos del día
CREATE FUNCTION fn_medicos_del_dia()
RETURNS INT
READS SQL DATA
BEGIN
    DECLARE v_medicos INT DEFAULT 0;
    SELECT COUNT(*)
      INTO v_medicos
      FROM medicos
     WHERE activo = 1
       AND en_guardia_hoy = 1;
    RETURN v_medicos;
END$$

-- Función 4: Porcentaje de ocupación de camas de una sala específica
CREATE FUNCTION fn_ocupacion_sala_pct(p_sala_id INT)
RETURNS DECIMAL(5,2)
READS SQL DATA
BEGIN
    DECLARE v_total INT DEFAULT 0;
    DECLARE v_ocupadas INT DEFAULT 0;

    SELECT COUNT(*), COALESCE(SUM(CASE WHEN ocupada = 1 THEN 1 ELSE 0 END), 0)
      INTO v_total, v_ocupadas
      FROM camas
     WHERE sala_id = p_sala_id;

    IF v_total = 0 THEN
        RETURN 0.00;
    END IF;

    RETURN ROUND((v_ocupadas * 100.0) / v_total, 2);
END$$

-- Función 5: Verifica si un paciente tiene una salida activa a laboratorio
CREATE FUNCTION fn_paciente_tiene_salida_activa(p_paciente_id INT)
RETURNS TINYINT(1)
READS SQL DATA
BEGIN
    DECLARE v_count INT DEFAULT 0;
    SELECT COUNT(*)
      INTO v_count
      FROM salidas_laboratorio
     WHERE paciente_id = p_paciente_id
       AND estado IN ('PendienteSalida', 'EnLaboratorio');
    RETURN IF(v_count > 0, 1, 0);
END$$

-- Procedimiento 1: Registra o actualiza paciente, crea su atención, ocupa cama y genera salida a laboratorio opcional
CREATE PROCEDURE sp_registrar_ingreso_paciente(
    IN p_cedula VARCHAR(25),
    IN p_nombres VARCHAR(140),
    IN p_sexo VARCHAR(15),
    IN p_sintomas VARCHAR(500),
    IN p_convenio_id INT,
    IN p_medico_id INT,
    IN p_cama_id INT,
    IN p_requiere_lab TINYINT(1),
    IN p_laboratorio_id INT,
    IN p_examen_lab VARCHAR(180),
    IN p_prioridad_lab VARCHAR(15),
    OUT p_paciente_id INT,
    OUT p_atencion_id INT
)
BEGIN
    DECLARE v_estado_atencion VARCHAR(20) DEFAULT 'EnAtencion';

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;

    -- Insertar paciente nuevo o actualizar sus síntomas actuales si ya existe su cédula
    INSERT INTO pacientes (cedula, nombres, sexo, sintomas_actuales, convenio_id)
    VALUES (p_cedula, p_nombres, p_sexo, p_sintomas, p_convenio_id)
    ON DUPLICATE KEY UPDATE
        nombres = VALUES(nombres),
        sexo = VALUES(sexo),
        sintomas_actuales = VALUES(sintomas_actuales),
        convenio_id = VALUES(convenio_id);

    SELECT id INTO p_paciente_id FROM pacientes WHERE cedula = p_cedula LIMIT 1;

    IF p_requiere_lab = 1 AND p_laboratorio_id IS NOT NULL THEN
        SET v_estado_atencion = 'EnLaboratorio';
    END IF;

    IF p_cama_id IS NOT NULL THEN
        UPDATE camas SET ocupada = 1 WHERE id = p_cama_id AND ocupada = 0;
    END IF;

    INSERT INTO atenciones (paciente_id, medico_id, cama_id, sintomas, requiere_laboratorio, estado)
    VALUES (p_paciente_id, p_medico_id, p_cama_id, p_sintomas, p_requiere_lab, v_estado_atencion);

    SET p_atencion_id = LAST_INSERT_ID();

    IF p_requiere_lab = 1 AND p_laboratorio_id IS NOT NULL THEN
        INSERT INTO salidas_laboratorio (
            atencion_id, paciente_id, medico_id, laboratorio_id,
            examen_solicitado, motivo_clinico, prioridad, estado
        )
        VALUES (
            p_atencion_id, p_paciente_id, p_medico_id, p_laboratorio_id,
            COALESCE(NULLIF(p_examen_lab, ''), 'Perfil Clínico General'),
            p_sintomas,
            COALESCE(NULLIF(p_prioridad_lab, ''), 'Urgente'),
            'EnLaboratorio'
        );
    END IF;

    COMMIT;
END$$

-- Procedimiento 2: Da de alta médica una atención y libera su cama automáticamente
CREATE PROCEDURE sp_dar_alta_atencion(IN p_atencion_id INT)
BEGIN
    DECLARE v_cama_id INT DEFAULT NULL;

    START TRANSACTION;

    SELECT cama_id INTO v_cama_id
      FROM atenciones
     WHERE id = p_atencion_id
     LIMIT 1;

    UPDATE atenciones
       SET estado = 'AltaMedica',
           fecha_egreso = CURRENT_TIMESTAMP
     WHERE id = p_atencion_id;

    IF v_cama_id IS NOT NULL THEN
        UPDATE camas SET ocupada = 0 WHERE id = v_cama_id;
    END IF;

    COMMIT;
END$$

DELIMITER ;

-- Vista consolidada de KPIs del Hospital
CREATE VIEW vw_indicadores_hospital AS
SELECT
    fn_camas_disponibles() AS camas_disponibles,
    (SELECT COUNT(*) FROM camas c INNER JOIN salas s ON s.id = c.sala_id WHERE s.activa = 1) AS camas_totales,
    fn_total_salas_activas() AS cantidad_salas,
    fn_medicos_del_dia() AS medicos_del_dia,
    (SELECT COUNT(*) FROM atenciones WHERE estado IN ('EnAtencion', 'EnLaboratorio')) AS pacientes_activos,
    (SELECT COUNT(*) FROM salidas_laboratorio WHERE estado IN ('PendienteSalida', 'EnLaboratorio')) AS salidas_laboratorio_activas;
