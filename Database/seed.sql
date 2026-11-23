-- ============================================================================
-- HospitalSystem - Datos Semilla (Seed) para Pruebas en MySQL
-- ============================================================================

USE hospital_system_db;

INSERT INTO hospitales (id, nombre, codigo_habilitacion, direccion, telefono) VALUES
(1, 'Hospital Central Metropolitano San Lucas', 'HCM-2026-01', 'Av. Libertador 1450, Sector Salud', '+58 212-555-8900');

INSERT INTO convenios (id, codigo, nombre, porcentaje_cobertura, activo) VALUES
(1, 'SEG-NAC', 'Seguro Nacional de Salud', 80.00, 1),
(2, 'CRUZ-AZUL', 'Previsión Médica Cruz Azul', 90.00, 1),
(3, 'PARTICULAR', 'Atención Particular Directa', 0.00, 1),
(4, 'CONV-BIOMED', 'Alianza Diagnóstica Biomédica', 75.00, 1);

-- 5 Salas Hospitalarias Activas
INSERT INTO salas (id, hospital_id, codigo, nombre, piso, activa) VALUES
(1, 1, 'SAL-URG', 'Urgencias y Triaje', 1, 1),
(2, 1, 'SAL-OBS', 'Observación Clínica', 1, 1),
(3, 1, 'SAL-MIN', 'Medicina Interna', 2, 1),
(4, 1, 'SAL-PED', 'Pediatría General', 2, 1),
(5, 1, 'SAL-UCI', 'Unidad de Cuidados Intensivos (UCI)', 3, 1);

-- 16 Camas distribuidas en las 5 salas (3 ocupadas inicialmente, 13 disponibles)
INSERT INTO camas (id, sala_id, codigo, ocupada, observacion) VALUES
(1,  1, 'URG-01', 1, 'Monitor multiparámetro'),
(2,  1, 'URG-02', 0, 'Toma de oxígeno central'),
(3,  1, 'URG-03', 0, 'Box de atención rápida'),
(4,  1, 'URG-04', 0, 'Box de reanimación'),
(5,  2, 'OBS-01', 1, 'Hidratación endovenosa'),
(6,  2, 'OBS-02', 0, 'Observación corta estancia'),
(7,  2, 'OBS-03', 0, 'Observación aislamiento ligero'),
(8,  3, 'MIN-01', 1, 'Habitación 201-A'),
(9,  3, 'MIN-02', 0, 'Habitación 201-B'),
(10, 3, 'MIN-03', 0, 'Habitación 202-A'),
(11, 3, 'MIN-04', 0, 'Habitación 202-B'),
(12, 4, 'PED-01', 0, 'Cuna clínica pediátrica'),
(13, 4, 'PED-02', 0, 'Cama juvenil con acompañante'),
(14, 4, 'PED-03', 0, 'Cama pediátrica monitorizada'),
(15, 5, 'UCI-01', 0, 'Ventilación mecánica avanzada'),
(16, 5, 'UCI-02', 0, 'Soporte hemodinámico completo');

-- Plantilla de Médicos (6 Médicos del Día en guardia hoy + 2 fuera de turno para probar filtros)
INSERT INTO medicos (id, cedula_profesional, nombres, especialidad, turno, consultorio, en_guardia_hoy, activo) VALUES
(1, 'MP-10452', 'Dra. Valentina Rojas Méndez', 'Medicina de Urgencias', 'Guardia24h', 'Urgencias Box 1', 1, 1),
(2, 'MP-18930', 'Dr. Alejandro Silva Paredes', 'Medicina Interna', 'Manana', 'Consultorio 204', 1, 1),
(3, 'MP-22415', 'Dra. Camila Herrera Soto', 'Cardiología Clínica', 'Manana', 'Consultorio 301', 1, 1),
(4, 'MP-30118', 'Dr. Sebastián Morales Vega', 'Infectología y Diagnóstico', 'Tarde', 'Consultorio 208', 1, 1),
(5, 'MP-33890', 'Dra. Lucía Fernández Castro', 'Pediatría', 'Tarde', 'Pabellón Pediátrico 2', 1, 1),
(6, 'MP-41207', 'Dr. Mateo Navarro Guzmán', 'Medicina Intensiva (UCI)', 'Noche', 'UCI Central Piso 3', 1, 1),
(7, 'MP-45901', 'Dr. Ricardo Mendoza Peña', 'Traumatología', 'Manana', 'Consultorio 105', 0, 1),
(8, 'MP-50234', 'Dra. Gabriela Ortiz Luna', 'Neurología', 'Tarde', 'Consultorio 310', 0, 1);

-- Catálogo base de diagnósticos CIE-10
INSERT INTO diagnosticos (id, codigo_cie10, descripcion) VALUES
(1, 'J18.9', 'Neumonía aguda en estudio'),
(2, 'A97.0', 'Sospecha de síndrome febril por dengue'),
(3, 'I20.0', 'Dolor torácico / angina inestable en evaluación'),
(4, 'K35.8', 'Dolor abdominal agudo en fosa ilíaca derecha'),
(5, 'R51',   'Cefalea intensa con signos de migraña');

-- Laboratorios para derivación / salidas de pacientes
INSERT INTO laboratorios (id, codigo, nombre, tipo, ubicacion, especialidad_analisis, convenio_id, activo) VALUES
(1, 'LAB-HEM', 'Laboratorio Central de Hematología y Bioquímica', 'Interno', 'Planta Baja - Ala Este', 'Hemograma, Perfil Renal, Electrolitos y Troponinas', NULL, 1),
(2, 'LAB-IMG', 'Unidad de Imagenología y Tomografía', 'Interno', 'Sótano 1 - Bloque Diagnóstico', 'Rayos X Digital, Ecografía y TAC Multicorte', NULL, 1),
(3, 'LAB-MIC', 'Laboratorio de Microbiología y Serología', 'Interno', 'Piso 1 - Ala Norte', 'Hemocultivos, PCR Viral, Antígeno Dengue', NULL, 1),
(4, 'LAB-EXT', 'Centro Biomédico Especializado (Externo)', 'ExternoConvenio', 'Av. Ciencias 220 (Ambulancia traslado)', 'Resonancia Magnética y Panel Inmunológico Avanzado', 4, 1);

-- Pacientes de ejemplo inicial
INSERT INTO pacientes (id, cedula, nombres, sexo, sintomas_actuales, convenio_id, fecha_registro) VALUES
(1, 'V-19845120', 'Carlos Eduardo Ramírez Tovar', 'Masculino', 'Fiebre alta 39.2°C de 3 días, mialgias intensas y dolor retroocular', 1, NOW() - INTERVAL 5 HOUR),
(2, 'V-24510988', 'Mariana Isabel Gómez Pérez', 'Femenino', 'Dolor opresivo en el pecho al esfuerzo, disnea leve y mareos', 2, NOW() - INTERVAL 3 HOUR),
(3, 'V-14203771', 'Jorge Luis Delgado Rivas', 'Masculino', 'Tos productiva persistente, escalofríos y saturación 93%', 1, NOW() - INTERVAL 2 HOUR);

-- Atenciones activas (ocupando las camas 1, 5 y 8)
INSERT INTO atenciones (id, paciente_id, medico_id, cama_id, sintomas, notas_medicas, requiere_laboratorio, estado, fecha_ingreso) VALUES
(1, 1, 4, 5, 'Fiebre alta 39.2°C de 3 días, mialgias intensas y dolor retroocular', 'Hidratación parenteral y control de plaquetas urgente', 1, 'EnLaboratorio', NOW() - INTERVAL 5 HOUR),
(2, 2, 3, 1, 'Dolor opresivo en el pecho al esfuerzo, disnea leve y mareos', 'ECG basal realizado, pendiente curva de troponinas', 1, 'EnLaboratorio', NOW() - INTERVAL 3 HOUR),
(3, 3, 2, 8, 'Tos productiva persistente, escalofríos y saturación 93%', 'Oxigenoterapia por cánula nasal y vigilancia respiratoria', 0, 'EnAtencion', NOW() - INTERVAL 2 HOUR);

INSERT INTO diagnosticos_paciente (atencion_id, paciente_id, diagnostico_id, confirmado) VALUES
(1, 1, 2, 0),
(2, 2, 3, 0),
(3, 3, 1, 1);

-- Salidas a laboratorios activas e históricas
INSERT INTO salidas_laboratorio (id, atencion_id, paciente_id, medico_id, laboratorio_id, examen_solicitado, motivo_clinico, prioridad, estado, resultado_resumen, fecha_salida, fecha_retorno) VALUES
(1, 1, 1, 4, 3, 'Hemograma con recuento plaquetario + Antígeno NS1 Dengue', 'Fiebre aguda y sospecha de trombocitopenia', 'Urgente', 'EnLaboratorio', NULL, NOW() - INTERVAL 4 HOUR, NULL),
(2, 2, 2, 3, 1, 'Troponina ultrasensible seriada + Perfil lipídico', 'Descartar síndrome coronario agudo', 'Critica', 'PendienteSalida', NULL, NOW() - INTERVAL 2 HOUR, NULL),
(3, 3, 3, 2, 2, 'Radiografía de Tórax PA y Lateral', 'Evaluar infiltrado basal derecho', 'Urgente', 'ResultadoRecibido', 'Infiltrado alveolar basal derecho compatible con neumonía comunitaria.', NOW() - INTERVAL 90 MINUTE, NOW() - INTERVAL 30 MINUTE);
