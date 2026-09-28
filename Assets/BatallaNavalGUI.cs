using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BatallaNavalGUI : MonoBehaviour
{
    [Header("Paneles de Navegación")]
    public GameObject panelMenuInicio;
    public GameObject panelJuego;

    [Header("Campos de Menú")]
    public TMP_InputField inputNombreJugador;
    public TMP_Text labelBienvenida;

    [Header("Referencias de Tableros (UI)")]
    public Transform gridJugador;  // "Tu Flota"
    public Transform gridEnemigo;  // "Radar Enemigo"
    public GameObject botonCasillaPrefab;
    public TMP_Text labelEstado;

    // Lógica de Doble Tablero (0 = Agua, 1 = Barco, 2 = Impacto, 3 = Fallo)
    private int[,] tableroJugador = new int[10, 10];
    private int[,] tableroEnemigo = new int[10, 10];

    private Image[,] visualJugador = new Image[10, 10];
    private Image[,] visualEnemigo = new Image[10, 10];

    // Configuración de Flota
    private int[] tamaniosBarcos = { 5, 4, 3, 3, 2 }; // Portaviones, Acorazado, etc.
    private int indiceBarcoActual = 0;
    private bool esHorizontal = true;
    private string nombreAlmirante = "Almirante";

    public enum EstadoJuego { Menu, FaseColocacion, TurnoJugador, TurnoEnemigo, FinJuego }
    private EstadoJuego estadoActual;

    void Start()
    {
        panelMenuInicio.SetActive(true);
        panelJuego.SetActive(false);
        estadoActual = EstadoJuego.Menu;
    }

    void Update()
    {
        // Alternar rotación del barco con Clic Derecho durante la colocación
        if (estadoActual == EstadoJuego.FaseColocacion && Input.GetMouseButtonDown(1))
        {
            esHorizontal = !esHorizontal;
            ActualizarMensajeEstado();
        }
    }

    // Método conectado al botón "INICIAR SECUENCIA"
    public void IniciarJuego()
    {
        if (!string.IsNullOrEmpty(inputNombreJugador.text))
            nombreAlmirante = inputNombreJugador.text;

        labelBienvenida.text = $"Base del Almirante: {nombreAlmirante}";

        panelMenuInicio.SetActive(false);
        panelJuego.SetActive(true);

        GenerarTablerosUI();
        CambiarEstado(EstadoJuego.FaseColocacion);
    }

    private void GenerarTablerosUI()
    {
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                // Crear casilla en "Tu Flota"
                GameObject bJugador = Instantiate(botonCasillaPrefab, gridJugador);
                visualJugador[x, y] = bJugador.GetComponent<Image>();
                int posX = x, posY = y;
                bJugador.GetComponent<Button>().onClick.AddListener(() => AlClicTableroJugador(posX, posY));

                // Crear casilla en "Radar Enemigo"
                GameObject bEnemigo = Instantiate(botonCasillaPrefab, gridEnemigo);
                visualEnemigo[x, y] = bEnemigo.GetComponent<Image>();
                bEnemigo.GetComponent<Button>().onClick.AddListener(() => AlClicTableroEnemigo(posX, posY));
            }
        }
    }

    private void AlClicTableroJugador(int x, int y)
    {
        if (estadoActual == EstadoJuego.FaseColocacion)
        {
            int tamanio = tamaniosBarcos[indiceBarcoActual];
            if (PuedeColocarBarco(tableroJugador, x, y, tamanio, esHorizontal))
            {
                ColocarBarco(tableroJugador, visualJugador, x, y, tamanio, esHorizontal);
                indiceBarcoActual++;

                if (indiceBarcoActual >= tamaniosBarcos.Length)
                {
                    CambiarEstado(EstadoJuego.TurnoJugador);
                }
                else
                {
                    ActualizarMensajeEstado();
                }
            }
        }
    }

    private void AlClicTableroEnemigo(int x, int y)
    {
        if (estadoActual == EstadoJuego.TurnoJugador && tableroEnemigo[x, y] < 2)
        {
            if (tableroEnemigo[x, y] == 1)
                tableroEnemigo[x, y] = 2; // Impacto
            else
                tableroEnemigo[x, y] = 3; // Fallo

            ActualizarCasillaVisual(visualEnemigo[x, y], tableroEnemigo[x, y]);
            CambiarEstado(EstadoJuego.TurnoEnemigo);
        }
    }

    private bool PuedeColocarBarco(int[,] tablero, int x, int y, int tamanio, bool horizontal)
    {
        for (int i = 0; i < tamanio; i++)
        {
            int nx = horizontal ? x : x + i;
            int ny = horizontal ? y + i : y;

            if (nx >= 10 || ny >= 10 || tablero[nx, ny] != 0)
                return false;
        }
        return true;
    }

    private void ColocarBarco(int[,] tablero, Image[,] visual, int x, int y, int tamanio, bool horizontal)
    {
        for (int i = 0; i < tamanio; i++)
        {
            int nx = horizontal ? x : x + i;
            int ny = horizontal ? y + i : y;

            tablero[nx, ny] = 1; // 1 = Barco
            visual[nx, ny].color = Color.gray;
        }
    }

    private void ActualizarCasillaVisual(Image img, int estado)
    {
        switch (estado)
        {
            case 2: img.color = Color.red; break;  // Impacto
            case 3: img.color = Color.cyan; break; // Agua
        }
    }

    private void CambiarEstado(EstadoJuego nuevoEstado)
    {
        estadoActual = nuevoEstado;
        ActualizarMensajeEstado();

        if (estadoActual == EstadoJuego.TurnoEnemigo)
            StartCoroutine(SimularTurnoEnemigo());
    }

    private void ActualizarMensajeEstado()
    {
        switch (estadoActual)
        {
            case EstadoJuego.FaseColocacion:
                string orientacion = esHorizontal ? "Horizontal" : "Vertical";
                labelEstado.text = $"Coloca barco de {tamaniosBarcos[indiceBarcoActual]} casillas ({orientacion}) - Clic Der. rotar";
                break;
            case EstadoJuego.TurnoJugador:
                labelEstado.text = "¡Selecciona una casilla en el Radar Enemigo para atacar!";
                break;
            case EstadoJuego.TurnoEnemigo:
                labelEstado.text = "El enemigo está calculando su disparo...";
                break;
        }
    }

    private IEnumerator SimularTurnoEnemigo()
    {
        yield return new WaitForSeconds(1.5f);
        CambiarEstado(EstadoJuego.TurnoJugador);
    }
}
