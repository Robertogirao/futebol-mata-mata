# Atualização da arena

## Alterações

- Adicionadas World Cup e Legends, cada uma com uma demo de 16 e uma cena de 32.
- Adicionados temas separados, campo de ano e progresso da World Cup por ano.
- Adicionadas quatro artes de estádio, goleiro e hélice, mais emblemas e monogramas.
- Renovados placar, cartões de um titular e banner de gol, com fontes e cores editáveis.
- Câmera de 6 para 7,2 e HUD em escala 0,85. Gráficos das paredes ocultados; física preservada.
- O elenco completo continua salvo; somente um jogador é selecionado por partida.
- Bola pontuada fica inativa imediatamente e só pode pontuar uma vez.
- Detecção de órbita e saída livre fora do giro substituem a tentativa baseada só em impulso.
- Timeout de bola parada do prefab ajustado de 0,5 para 8 segundos.

## Verificações executadas

- Extração integral dos dois volumes RAR: 991 arquivos.
- Análise sintática Roslyn/C# 9 de 26 arquivos C#: zero erros.
- Compilação de compatibilidade dos scripts de runtime contra referências Unity 2021/UI,
  com aliases das APIs que mudaram no Unity 6 e declarações substitutas de TMP: zero erros.
  Isso confere tipos e conexões de código, mas não substitui uma compilação no Unity 6000.4.
- Modelo C# de chaveamento executado: progressão completa e reabertura para 2, 4, 8, 16 e 32;
  progressão com BYE também passou.
- Conferência YAML: GUIDs únicos, nenhuma nova referência pendente e assets com `.meta`.
- Cenas novas: 16/32 participantes distintos conforme a chave; cada ficha contém um jogador,
  bandeira e foto ou monograma válido.
- Câmera, materiais e sprites dos oito temas conferidos.
- Colliders e Rigidbody2D das oito cenas de mata-mata comparados com as cenas de origem:
  nenhuma alteração de forma ou parâmetros físicos.

## Ainda depende do editor Unity

O Unity não está instalado neste ambiente. Não foram executados Play Mode, build,
renderização da HUD ou simulação física do Unity. Os oito testes PlayMode incluídos
precisam ser rodados no Unity Test Runner. A confirmação final da aparência e da
correção de recapturas da bola exige essa execução.

## Compatibilidade

Versão do projeto preservada: Unity 6000.4.11f1. Pacotes e assets originais preservados.
As cenas originais de 32 mantêm os participantes e vagas do backup. Novas fichas são
exemplos editáveis, não uma lista oficial de classificados de uma Copa específica.
O limite total de seis bolas e o ritmo de lançamentos do backup foram mantidos.


## Atualização: mãos animadas e publicação no GitHub

- A malha de arte do goleiro agora permite balanço das luvas, ajuste à bola próxima
  e reação curta ao contato. O sprite original compartilhado não é modificado.
- A deformação é ponderada nos braços e luvas, mantendo a base do corpo parada.
- A animação usa tempo do jogo e congela na pausa. Ativação, amplitude, ritmo
  e reação podem ser editados no tema. Colisores e IA permanecem inalterados.
- Incluídos dois testes PlayMode adicionais para peso dos braços, movimento da malha,
  preservação do colisor e congelamento da animação.
- A prévia GIF usa uma reprodução WebGL da malha e não é uma gravação do Unity.
- O shader, a renderização URP e os oito testes PlayMode ainda precisam ser validados
  no editor Unity 6000.4.11f1, indisponível neste ambiente.
