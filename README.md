# VOID — v1.0.0-pre-alpha

> Chat em tempo real. Simples, rápido, sem rastreamento.

---

## ⬇️ Download

Baixa a última versão em [Releases](https://github.com/dripperofc/VOID/releases).

1. Descarrega o `Void-*.zip`
2. Extrai em qualquer pasta
3. Executa o `Void.exe` — não precisa instalar nada
4. Cria uma conta e começa a usar

---

## ✅ O que funciona

- **Mensagens diretas (DMs)** — chat privado entre utilizadores em tempo real
- **Chamadas de voz** — chamadas 1 para 1 funcionando
- **Sistema de amigos** — adicionar, aceitar e remover amigos
- **Login e registo** — contas com senha encriptada (BCrypt + JWT)
- **Perfil customizável** — nickname e cor do avatar

---

## 🌐 Versão Web

VOID também tem uma versão pra browser:

[**VOID_WEB**](https://github.com/dripperofc/VOID_WEB) — HTML/CSS/JS puro, abre direto no navegador.

---

## 🔧 Como buildar (dev)

**Requisitos:**
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows (builds Linux/macOS em breve)

```bash
dotnet restore
dotnet run
```

**Build release Windows:**
```bash
dotnet publish Void.csproj -c Release -r win-x64 --self-contained -o ./publish
```

---

## ⚠️ Bugs conhecidos

- **Servidores quebrados** — a funcionalidade de servidores e canais está temporariamente fora de serviço
- **Status de utilizador** — o indicador online/offline pode não refletir o estado real em alguns casos

---

## 🚧 Roadmap

- [ ] Correção do sistema de servidores
- [ ] Status online/offline estável
- [ ] Histórico de mensagens persistente
- [ ] Notificações (desktop + web)
- [ ] Menções @utilizador
- [ ] Builds Linux e macOS
- [ ] Chamadas de voz WebRTC (web)
- [ ] Câmera e compartilhar ecrã

---

## 🐛 Como reportar issue

1. Vê se já foi reportada em [issues](https://github.com/dripperofc/VOID/issues)
2. Abre nova issue com:
   - O que aconteceu
   - O que esperavas que acontecesse
   - Log de erro (se houver)

---

*VOID Project © 2026 — Licença MIT*
