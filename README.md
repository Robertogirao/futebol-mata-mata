# Basquete 2D — entrada na rede V7

Esta atualização faz a bolinha aparecer por trás do aro e dos fios quando entra na cesta. Ao sair da rede, ela volta à frente da cena. A mudança é visual: a física, os colisores e a contagem dos pontos continuam como antes.

O pacote inclui também o acabamento visual V6 do funil, da rampa e das cestas. Ele é uma atualização para a cena **Basquete 2D — Funil e Duas Cestas**, com a versão V5 ou V6 já instalada; não é o projeto completo.

## Como instalar

1. Feche o Unity e extraia o ZIP baixado do GitHub.
2. Entre na pasta extraída `futebol-mata-mata-basquete2d-rede-v7` e copie a pasta **Assets** para a raiz do projeto NBA. Junte as pastas e substitua os arquivos solicitados, inclusive os `.meta`.
3. Abra o projeto, aguarde a importação e use **Tools > Basquete 2D > Abrir funil de duas cestas**.
4. Selecione `MarbleBasketFunil2D`. No Inspector, confira **Entrada na rede V7** e dê Play.

O pacote não substitui a cena `.unity` nem o fundo da arena. Suas configurações salvas são preservadas.

## Verificações e limites

Os métodos de física, colisão, movimentação e regras foram comparados com a V6; a alteração é na ordem de desenho. Os scripts de runtime compilaram sem erros ou avisos com referências da Unity 2021.3.33. O pacote inclui testes de PlayMode para as duas cestas.

O Unity Editor não está disponível neste ambiente. A compilação na Unity 6000.4.11f1, os testes de PlayMode e a aparência durante a partida ainda precisam ser conferidos no seu projeto.
