using UnityEngine;

public class GoalDetector : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D outro)
    {
        // 1. TRAVA DE SEGURANÇA MÁXIMA: Se a UI não existir ou a partida NÃO estiver rolando (já acabou ou congelou), ignora a bola na hora!
        if (UniversalUI.Instance == null || !UniversalUI.Instance.partidaEmAndamento)
        {
            return;
        }

        BallController bola = outro.GetComponentInParent<BallController>();
        
        if (bola != null && UniversalUI.Instance.PodeRegistrarGol)
        {
            // Captura os dados antes de desativar a bola. A trava fica na bola,
            // compartilhada por sensores e por todos os seus colliders filhos.
            JogadorDados jogador = bola.dadosJogador;
            Color cor = bola.visualBola != null ? bola.visualBola.color : Color.white;
            bool timeA = bola.pertenceAoTimeA;
            if (!bola.ConsumirNoGol()) return;

            if (UniversalUI.Instance != null)
            {
                UniversalUI.Instance.RegistrarGol(timeA, 0);
            }

            if (GoalBannerUI.Instance != null)
            {
                GoalBannerUI.Instance.ExibirBannerDeGol(jogador, cor);
            }
        }
    }
}
