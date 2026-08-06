/*=========================================================
  POLÍTICAS RLS - CONTROL DE GASTOS
=========================================================*/

/*=========================================================
  1. CATÁLOGO DE CATEGORÍAS
=========================================================*/

create policy "Usuarios autenticados consultan categorias"
on finanzas.tbt_categ_gasto
for select
to authenticated
using (true);


/*=========================================================
  2. CATÁLOGO DE MEDIOS DE PAGO
=========================================================*/

create policy "Usuarios autenticados consultan medios de pago"
on finanzas.tbt_medio_pago
for select
to authenticated
using (true);


/*=========================================================
  3. CATÁLOGO DE ESTADOS
=========================================================*/

create policy "Usuarios autenticados consultan estados"
on finanzas.tbt_estad_gasto
for select
to authenticated
using (true);


/*=========================================================
  4. PERFIL
=========================================================*/

create policy "Usuario consulta su perfil"
on finanzas.tbm_perfil
for select
to authenticated
using
(
    auth.uid() = idd_usuario
);

create policy "Usuario registra su perfil"
on finanzas.tbm_perfil
for insert
to authenticated
with check
(
    auth.uid() = idd_usuario
);

create policy "Usuario actualiza su perfil"
on finanzas.tbm_perfil
for update
to authenticated
using
(
    auth.uid() = idd_usuario
)
with check
(
    auth.uid() = idd_usuario
);


/*=========================================================
  5. GASTOS
=========================================================*/

create policy "Usuario consulta sus gastos"
on finanzas.tbm_gasto
for select
to authenticated
using
(
    auth.uid() = idd_usuario
);

create policy "Usuario registra sus gastos"
on finanzas.tbm_gasto
for insert
to authenticated
with check
(
    auth.uid() = idd_usuario
);

create policy "Usuario actualiza sus gastos"
on finanzas.tbm_gasto
for update
to authenticated
using
(
    auth.uid() = idd_usuario
)
with check
(
    auth.uid() = idd_usuario
);

create policy "Usuario elimina sus gastos"
on finanzas.tbm_gasto
for delete
to authenticated
using
(
    auth.uid() = idd_usuario
);