# 🌌 VOID — Roadmap Oficial (v0.8.7)
*"Do chat ao hub de comunicação definitivo"*

---

## 🎯 Filosofia
Começar simples. Fazer funcionar. Depois fazer bonito. Depois fazer escalar.
Cada fase entrega valor real antes de avançar para a próxima.

---

## ✅ FASE 0 — Base (onde estamos agora)
**Objetivo:** App funcional no Windows, compilando sem erros.

- [x] Arquitetura MVVM com CommunityToolkit
- [x] Interface dark inspirada no Discord
- [x] Login e registro com salvamento local (JSON)
- [x] Chat em tempo real via SignalR
- [x] DMs separadas de canais de servidor
- [x] Sistema de amigos (adicionar/remover)
- [x] Controles de áudio (mute/deafen — UI)
- [x] VoidServer funcional com histórico de mensagens

---

## 🚧 FASE 1 — Fundação sólida (próximo passo)
**Objetivo:** Tudo que existe funciona de verdade, sem gambiarras.

### Chat
- [ ] Scroll automático para última mensagem ao receber nova
- [ ] Indicador de "digitando..." em DMs
- [ ] Timestamps com data quando mensagem é de outro dia
- [ ] Notificação visual (badge) em conversas com mensagens não lidas

### Identidade
- [ ] Tela de perfil — editar nickname, cor do avatar
- [ ] Avatar com iniciais usando a cor definida no registro
- [ ] Status manual (Online / Ausente / Não Perturbe / Invisível)

### Servidor
- [ ] Criar servidor localmente
- [ ] Criar canais dentro do servidor
- [ ] Persistir lista de servidores entre sessões (JSON local)

### Segurança
- [ ] Hash de senha com SHA-256 no AuthenticationService
  (simples mas muito melhor que texto puro)

### Qualidade
- [ ] Tratar desconexão do SignalR com reconexão automática e aviso na UI
- [ ] Limitar tamanho da mensagem (500 chars) com contador visual

---

## 🔜 FASE 2 — Experiência
**Objetivo:** Usar o VOID seja agradável de verdade.

### UI/UX
- [ ] Animação de entrada nas mensagens (fade in suave)
- [ ] Tema customizável — cor de acento escolhida pelo usuário
- [ ] Modo compacto (mensagens mais densas, sem avatar em cada linha)
- [ ] Markdown básico nas mensagens: **negrito**, *itálico*, `código`
- [ ] Emoji picker básico

### Notificações
- [ ] Som de notificação ao receber mensagem (NAudio já instalado)
- [ ] Som de entrada/saída de usuários no servidor
- [ ] Notificações nativas do Windows (toast)

### Histórico
- [ ] Carregar histórico do canal ao entrar nele (backend já suporta)
- [ ] Busca simples em mensagens do canal atual

---

## 🔜 FASE 3 — Voz (VoIP)
**Objetivo:** Canais de voz funcionando no PC.
*(NAudio já está instalado — base pronta)*

- [ ] Captura de microfone com NAudio (WaveIn)
- [ ] Transmissão de áudio via SignalR (chunks de PCM/Opus)
- [ ] Reprodução de áudio recebido (WaveOut)
- [ ] Canal de voz: entrar/sair com indicador visual de quem está falando
- [ ] Mute e deafen funcionando de verdade (não só na UI)
- [ ] Supressão de ruído básica (filtro de passa-alta)

---

## 🔜 FASE 4 — Moderação e IA
**Objetivo:** Servidores com estrutura real.

- [ ] Sistema de cargos (Admin, Moderador, Membro)
- [ ] Permissões por cargo (ver canal, enviar mensagem, etc.)
- [ ] Banimento e silenciamento de usuário
- [ ] Filtro de palavras configurável por servidor
- [ ] Moderação assistida por IA — integração com API de classificação
  de texto para detectar conteúdo tóxico automaticamente

---

## 🔜 FASE 5 — Cross-platform
**Objetivo:** Mesmo app, todos os sistemas.
*(Avalonia já suporta tudo isso nativamente)*

- [ ] Testar e corrigir bugs no Linux (Ubuntu/Arch)
- [ ] Testar e corrigir bugs no macOS
- [ ] Build automatizado para Windows/Linux/macOS via GitHub Actions
- [ ] Android e iOS via Avalonia.Android / Avalonia.iOS
  (layout responsivo, touch-friendly)
- [ ] WebAssembly via Avalonia.Web (acesso pelo browser sem instalar)

---

## 🔭 FASE 6 — Futuro distante
**Objetivos ambiciosos para quando a base estiver sólida.**

- [ ] Criptografia de mensagens (E2E) resistente a computação quântica
      (algoritmos pós-quânticos: CRYSTALS-Kyber/Dilithium)
- [ ] Shaders e efeitos visuais customizados por servidor
- [ ] Realidade aumentada em chamadas mobile (filtros, avatares 3D)
- [ ] Suporte nativo a WebRTC para voz P2P sem servidor intermediário
- [ ] Processamento de áudio espacial (3D audio em canais de voz)
- [ ] API pública para bots e integrações externas

---

## 📋 Backlog (ideias sem fase definida)
- Sistema de reações em mensagens (emojis)
- Compartilhamento de arquivos e imagens
- Chamadas de vídeo em DMs
- Tela de descoberta de servidores públicos
- App de configurações completo
- Modo streamer (ocultar informações sensíveis)
- Integração com Spotify / Last.fm (status musical)
- Plugins e extensões de terceiros

---

## 🗓️ Prioridade imediata
**Terminar Fase 1 antes de qualquer outra coisa.**
Uma base sólida vale mais que dez features pela metade.
