using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PopUpController : MonoBehaviour
{
    public GameObject painelPopUp;
    public TMP_Text mensagemTexto;
    public Button botao1;
    public Button botao2;

    void Start()
    {
        painelPopUp.SetActive(false);
    }

    /// Mostra o pop-up com 1 botão
    public void Mostrar(string mensagem, string textoBotao, System.Action acaoBotao)
    {
        painelPopUp.SetActive(true);
        mensagemTexto.text = mensagem;

        // Configurar Botão 1
        botao1.gameObject.SetActive(true);
        botao1.GetComponentInChildren<TMP_Text>().text = textoBotao;
        botao1.onClick.RemoveAllListeners();
        botao1.onClick.AddListener(() =>
        {
            Fechar();           // Fecha primeiro
            acaoBotao?.Invoke(); // Depois executa a ação (ex: abrir próximo pop-up)
        });

        // Esconder Botão 2
        botao2.gameObject.SetActive(false);
    }

    /// Mostra o pop-up com 2 botões
    public void Mostrar(string mensagem,
                        string textoBotao1, System.Action acao1,
                        string textoBotao2, System.Action acao2)
    {
        painelPopUp.SetActive(true);
        mensagemTexto.text = mensagem;

        // Configurar Botão 1
        botao1.gameObject.SetActive(true);
        botao1.GetComponentInChildren<TMP_Text>().text = textoBotao1;
        botao1.onClick.RemoveAllListeners();
        botao1.onClick.AddListener(() =>
        {
            Fechar();
            acao1?.Invoke();
        });

        // Configurar Botão 2
        botao2.gameObject.SetActive(true);
        botao2.GetComponentInChildren<TMP_Text>().text = textoBotao2;
        botao2.onClick.RemoveAllListeners();
        botao2.onClick.AddListener(() =>
        {
            Fechar();
            acao2?.Invoke();
        });
    }

    /// Fecha o pop-up
    public void Fechar()
    {
        painelPopUp.SetActive(false);
    }
}
