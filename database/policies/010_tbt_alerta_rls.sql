alter table finanzas.tbt_alerta enable row level security;

drop policy if exists "Usuarios gestionan sus alertas" on finanzas.tbt_alerta;
create policy "Usuarios gestionan sus alertas"
on finanzas.tbt_alerta
for all
to authenticated
using (auth.uid() = idd_usuario)
with check (auth.uid() = idd_usuario);
