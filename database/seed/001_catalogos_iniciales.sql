insert into finanzas.tbt_categ_gasto
(
    nom_categ_gasto
)
values
    ('Alimentación'),
    ('Transporte'),
    ('Entretenimiento'),
    ('Servicios'),
    ('Compras'),
    ('Salud'),
    ('Educación'),
    ('Vivienda'),
    ('Otros');

insert into finanzas.tbt_medio_pago
(
    nom_medio_pago
)
values
    ('Yape'),
    ('Plin'),
    ('Efectivo'),
    ('Tarjeta de débito'),
    ('Tarjeta de crédito'),
    ('Transferencia');

insert into finanzas.tbt_estad_gasto
(
    cod_estado,
    nom_estado
)
values
    ('BORRADOR', 'Borrador'),
    ('PENDIENTE', 'Pendiente de revisión'),
    ('CONFIRMADO', 'Confirmado'),
    ('ANULADO', 'Anulado');