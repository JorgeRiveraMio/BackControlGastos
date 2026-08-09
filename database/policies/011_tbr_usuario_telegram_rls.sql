alter table finanzas.tbr_usuario_telegram enable row level security;
drop policy if exists "Usuarios gestionan su Telegram" on finanzas.tbr_usuario_telegram;
create policy "Usuarios gestionan su Telegram" on finanzas.tbr_usuario_telegram for all to authenticated using (auth.uid()=idd_usuario) with check (auth.uid()=idd_usuario);
