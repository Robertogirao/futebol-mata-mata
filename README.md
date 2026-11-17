# Basquete 2D — máquina de arremesso V9

A proposta da máquina de arremesso foi aplicada à cena: o funil ganhou suportes, o eixo ganhou uma base de motor e a alavanca tem um novo braço com ponta de basquete.

As novas peças são visuais. O eixo, o tamanho e a colisão da alavanca, a abertura lateral do funil, os movimentos, a pontuação e a rede interativa da V8 foram preservados. O pacote é uma atualização para a cena **Basquete 2D — Funil e Duas Cestas** com a V8 já instalada; não é o projeto completo.

## Como instalar

1. Feche o Unity e extraia o ZIP baixado do GitHub.
2. Abra a pasta extraída `futebol-mata-mata-basquete2d-maquina-v9` e copie a pasta **Assets** para a raiz do projeto NBA. Junte as pastas e substitua os arquivos solicitados, inclusive os `.meta`.
3. Abra o projeto, aguarde a importação e use **Tools > Basquete 2D > Abrir funil de duas cestas**.
4. Selecione `MarbleBasketFunil2D`. No Inspector, confira **Máquina de arremesso V9** e dê Play.

O pacote não substitui a cena `.unity` nem o fundo da arena. Suas configurações salvas são preservadas.

## Verificações e limites

Os métodos de física, colisão, movimento, pontuação e rede foram comparados com a V8 e preservados. Os scripts de runtime compilaram sem erros ou avisos com referências da Unity 2021.3.33. O pacote inclui um teste de PlayMode para a arte da máquina e os testes da rede.

O Unity Editor não está disponível neste ambiente. A compilação na Unity 6000.4.11f1, os testes de PlayMode e o encaixe do novo visual durante a partida ainda precisam ser conferidos no seu projeto.
