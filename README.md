# Basquete 2D — rede interativa V8

Depois de marcar a cesta, a bolinha perde parte da velocidade e encontra as laterais físicas da rede antes de sair por baixo. A rede também balança quando a bola passa. É um balanço simples com laterais físicas, não uma simulação completa de tecido.

As laterais são ativadas para a bola depois da pontuação; assim, a trajetória dos arremessos antes da cesta continua como na V7. A contagem de pontos, o funil, a alavanca, a barreira, o chão e a lava são preservados.

O pacote inclui o acabamento visual V6 e a ordem de desenho da V7. É uma atualização para a cena **Basquete 2D — Funil e Duas Cestas** com a V5, V6 ou V7 já instalada; não é o projeto completo. A imagem da máquina de arremesso mostrada na conversa é um estudo visual e não substitui o funil nesta versão.

## Como instalar

1. Feche o Unity e extraia o ZIP baixado do GitHub.
2. Abra a pasta extraída `futebol-mata-mata-basquete2d-rede-fisica-v8` e copie a pasta **Assets** para a raiz do projeto NBA. Junte as pastas e substitua os arquivos solicitados, inclusive os `.meta`.
3. Abra o projeto, aguarde a importação e use **Tools > Basquete 2D > Abrir funil de duas cestas**.
4. Selecione `MarbleBasketFunil2D`. No Inspector, confira **Rede interativa V8** e dê Play.

O pacote não substitui a cena `.unity` nem o fundo da arena. Suas configurações salvas são preservadas.

## Verificações e limites

Um modelo externo aproximado em Box2D exercitou 140 combinações de entrada na rede e 20 partidas com 10 ou 12 bolinhas. No modelo, as duas equipes pontuaram nas duas cestas e as bolinhas puderam sair por baixo da rede. Uma recuperação de segurança ocorreu em uma das simulações com 12 bolinhas. O modelo não substitui o Play no Unity.

Os scripts de runtime compilaram sem erros ou avisos com referências da Unity 2021.3.33. O pacote inclui testes de PlayMode para as duas redes e para a ordem de desenho.

O Unity Editor não está disponível neste ambiente. A compilação na Unity 6000.4.11f1, os testes de PlayMode e o movimento da rede durante a partida ainda precisam ser conferidos no seu projeto.
