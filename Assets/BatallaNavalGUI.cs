using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BatallaNavalGUI : MonoBehaviour
{
    public LogicaBot botEnemigo;
    
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
    public TMP_Text labelResultados;

    // Lógica de Doble Tablero 
    private int[,] tableroJugador = new int[10, 10];
    
    // Matrices visuales
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

        // Reinicio rápido al final del juego con una tecla
        if (estadoActual == EstadoJuego.FinJuego && Input.GetKeyDown(KeyCode.R))
        {
            ReiniciarPartida();
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

    // Único método AlClicTableroEnemigo integrado con tu sistema de turnos
    public void AlClicTableroEnemigo(int x, int y)
    {
        // Solo permite clics si es el turno del jugador
        if (estadoActual != EstadoJuego.TurnoJugador) return;

        // 1. Verificamos si ya habías disparado en esta coordenada
        if (botEnemigo.tableroEnemigo[x, y] == 2 || botEnemigo.tableroEnemigo[x, y] == -1)
        {
            return; // Ya disparaste aquí, ignoramos el clic
        }
        // Traducimos tus coordenadas
        string coordTexto = TraducirCoordenadas(x, y);

        // 2. Revisamos si hay barco (1) o agua (0) en la lógica del bot
        if (botEnemigo.tableroEnemigo[x, y] == 1) 
        {
            visualEnemigo[x, y].color = Color.red; 
            botEnemigo.tableroEnemigo[x, y] = 2;   
            labelResultados.text = $"¡Tiro Certero! Impactaste un barco enemigo en {coordTexto}.";
        }
        else if (botEnemigo.tableroEnemigo[x, y] == 0) 
        {
            visualEnemigo[x, y].color = Color.cyan; 
            botEnemigo.tableroEnemigo[x, y] = -1;    
            labelResultados.text = $"Fallaste. Disparo al agua en {coordTexto}.";
        }

        if (VerificarVictoriaODerrota())
            return;

        // 3. Cambiamos al turno del enemigo (inicia la corrutina de espera)
        CambiarEstado(EstadoJuego.TurnoEnemigo);
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
            case EstadoJuego.FinJuego:
                labelEstado.text = "Partida finalizada";
                break;
        }
    }

    private IEnumerator SimularTurnoEnemigo()
    {
        // El bot "piensa" por 1.5 segundos
        yield return new WaitForSeconds(1.5f);
        
        // El bot dispara
        RecibirDisparoDelBot();

        if (estadoActual == EstadoJuego.FinJuego)
            yield break;

        // Se le devuelve el turno al jugador
        CambiarEstado(EstadoJuego.TurnoJugador);
    }

    // Único método de disparo del bot usando la matriz visual
    private void RecibirDisparoDelBot()
    {
        Vector2Int coordenadasAtaque = botEnemigo.TurnoDeAtaqueBot();
        int botX = coordenadasAtaque.x;
        int botY = coordenadasAtaque.y;

        // Traducimos las coordenadas antes de imprimirlas
        string coordTexto = TraducirCoordenadas(botX, botY); 

        if (visualJugador[botX, botY].color == Color.gray) 
        {
            visualJugador[botX, botY].color = Color.red;
            tableroJugador[botX, botY] = 2;
            labelResultados.text = $"¡Alerta! El enemigo impactó tu barco en {coordTexto}."; 
        }
        else
        {
            visualJugador[botX, botY].color = Color.cyan; 
            labelResultados.text = $"El enemigo falló. Disparó al agua en {coordTexto}."; 
        }

        VerificarVictoriaODerrota();
    }

    private bool VerificarVictoriaODerrota()
    {
        int barcosEnemigosRestantes = ContarBarcosRestantes(botEnemigo.tableroEnemigo);
        int barcosJugadorRestantes = ContarBarcosRestantes(tableroJugador);

        if (barcosEnemigosRestantes == 0)
        {
            estadoActual = EstadoJuego.FinJuego;
            labelEstado.text = "¡Victoria!";
            labelResultados.text = $"¡Has hundido toda la flota enemiga, {nombreAlmirante}!";
            return true;
        }

        if (barcosJugadorRestantes == 0)
        {
            estadoActual = EstadoJuego.FinJuego;
            labelEstado.text = "¡Derrota!";
            labelResultados.text = "La flota ha sido destruida. Inténtalo de nuevo.";
            return true;
        }

        return false;
    }

    private int ContarBarcosRestantes(int[,] tablero)
    {
        int contador = 0;

        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                if (tablero[x, y] == 1)
                    contador++;
            }
        }

        return contador;
    }

    private void ReiniciarPartida()
    {
        // Reinicia el estado del juego
        tableroJugador = new int[10, 10];
        indiceBarcoActual = 0;
        esHorizontal = true;
        nombreAlmirante = string.IsNullOrEmpty(inputNombreJugador.text) ? "Almirante" : inputNombreJugador.text;

        // Reinicia el bot enemigo
        if (botEnemigo != null)
        {
            botEnemigo.GenerarFlotaEnemiga();
            botEnemigo.memoriaDisparosBot = new int[10, 10];
        }

        // Limpia el tablero visual
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                visualJugador[x, y].color = Color.white;
                visualEnemigo[x, y].color = Color.white;
            }
        }

        labelResultados.text = "Partida reiniciada. Nueva estrategia enemiga.";
        estadoActual = EstadoJuego.FaseColocacion;
        ActualizarMensajeEstado();
    }

    private string TraducirCoordenadas(int fila, int columna)
    {
        char letra = (char)('A' + columna); // Convierte el 0 en A, 1 en B, etc.
        int numero = fila + 1;              // Convierte el 0 en 1, 1 en 2, etc.
        return $"{letra}{numero}";
    }
}