using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopUpController : MonoBehaviour
{
    public GameObject painelPopUp;
    public TMP_Text tituloTexto;
    public TMP_Text mensagemTexto;
    public Button botao1;
    public Button botao2;

    void Start()
    {
        painelPopUp.SetActive(false);
    }

    public void Mostrar(string titulo, string mensagem, string textoBotao, System.Action acaoBotao)
    {
        painelPopUp.SetActive(true);

        if (tituloTexto != null)
            tituloTexto.text = titulo;

        mensagemTexto.text = mensagem;

        botao1.gameObject.SetActive(true);
        botao1.GetComponentInChildren<TMP_Text>().text = textoBotao;
        botao1.onClick.RemoveAllListeners();
        botao1.onClick.AddListener(() =>
        {
            Fechar();
            acaoBotao?.Invoke();
        });

        botao2.gameObject.SetActive(false);
    }

    public void Mostrar(
        string titulo,
        string mensagem,
        string textoBotao1,
        System.Action acao1,
        string textoBotao2,
        System.Action acao2
    )
    {
        painelPopUp.SetActive(true);

        if (tituloTexto != null)
            tituloTexto.text = titulo;

        mensagemTexto.text = mensagem;

        botao1.gameObject.SetActive(true);
        botao1.GetComponentInChildren<TMP_Text>().text = textoBotao1;
        botao1.onClick.RemoveAllListeners();
        botao1.onClick.AddListener(() =>
        {
            Fechar();
            acao1?.Invoke();
        });

        botao2.gameObject.SetActive(true);
        botao2.GetComponentInChildren<TMP_Text>().text = textoBotao2;
        botao2.onClick.RemoveAllListeners();
        botao2.onClick.AddListener(() =>
        {
            Fechar();
            acao2?.Invoke();
        });
    }

    public void Fechar()
    {
        painelPopUp.SetActive(false);
    }
}
