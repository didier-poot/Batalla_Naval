using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic; // Necesario para las listas
using UnityEngine.SceneManagement;

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
    public Transform gridJugador;
    public Transform gridEnemigo;
    public GameObject botonCasillaPrefab;
    public TMP_Text labelEstado;
    public TMP_Text labelResultados;

    [Header("Assets Visuales")]
    public Sprite spriteAgua;      
    public Sprite spriteFallo;     // Si lo tienes (ej. una "X"). Si no, dejará la salpicadura.
    
    [System.Serializable]
    public struct DiseñoBarco
    {
        public Sprite proa;
        public Sprite cuerpo;
        public Sprite popa;
    }
    
    [Header("Efectos de Impacto y Agua")]
    public Sprite[] animacionExplosion; 
    public Sprite[] animacionFuegoLoop; // <-- AQUÍ ARRASTRA TUS 2 SPRITES DE FUEGO
    public Sprite[] animacionAgua;      

    [Header("Diseños de la Flota")]
    public DiseñoBarco[] diseñosFlota;

    private int[,] tableroJugador = new int[10, 10];
    
    private Image[,] visualJugador = new Image[10, 10];
    private Image[,] visualEnemigo = new Image[10, 10];

    private int[] tamaniosBarcos = { 5, 4, 3, 3, 2 }; 
    private int indiceBarcoActual = 0;
    private bool esHorizontal = true;
    private string nombreAlmirante = "Almirante";

    public enum EstadoJuego { Menu, FaseColocacion, TurnoJugador, TurnoEnemigo, FinJuego }
    private EstadoJuego estadoActual;
    
    [Header("Fin de Juego")]
    public GameObject panelFinJuego;
    public TMP_Text textoFinJuego;
    
    private int impactosParaGanar = 17; 
    private int aciertosJugador = 0;
    private int aciertosBot = 0;

    void Start()
    {
        panelMenuInicio.SetActive(true);
        panelJuego.SetActive(false);
        if (panelFinJuego != null) panelFinJuego.SetActive(false);
        estadoActual = EstadoJuego.Menu;
    }

    void Update()
    {
        if (estadoActual == EstadoJuego.FaseColocacion && Input.GetMouseButtonDown(1))
        {
            esHorizontal = !esHorizontal;
            ActualizarMensajeEstado();
        }

        if (estadoActual == EstadoJuego.FinJuego && Input.GetKeyDown(KeyCode.R))
        {
            ReiniciarPartida();
        }
    }

    public void IniciarJuego()
    {
        if (!string.IsNullOrEmpty(inputNombreJugador.text))
            nombreAlmirante = inputNombreJugador.text;

        labelBienvenida.text = $"Base del Almirante: {nombreAlmirante}";

        panelMenuInicio.SetActive(false);
        panelJuego.SetActive(true);

        GenerarTablerosUI();
        
        // Mapea los barcos enemigos invisibles para poder revelarlos al disparar
        GenerarVisualesEnemigosOcultos();
        
        CambiarEstado(EstadoJuego.FaseColocacion);
    }

    private void GenerarTablerosUI()
    {
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                // Casilla "Tu Flota"
                GameObject bJugador = Instantiate(botonCasillaPrefab, gridJugador);
                visualJugador[x, y] = bJugador.GetComponent<Image>();
                visualJugador[x, y].sprite = spriteAgua; 
                
                int posX = x, posY = y;
                bJugador.GetComponent<Button>().onClick.AddListener(() => AlClicTableroJugador(posX, posY));

                // Casilla "Radar Enemigo"
                GameObject bEnemigo = Instantiate(botonCasillaPrefab, gridEnemigo);
                visualEnemigo[x, y] = bEnemigo.GetComponent<Image>();
                visualEnemigo[x, y].sprite = spriteAgua; 
                
                bEnemigo.GetComponent<Button>().onClick.AddListener(() => AlClicTableroEnemigo(posX, posY));
            }
        }
    }

    // Novedad: Deduce la posición del barco enemigo y le asigna el asset (pero invisible)
    private void GenerarVisualesEnemigosOcultos()
    {
        // Usamos .Count en lugar de .Length porque vidaBarcos es un Diccionario
        for (int id = 1; id <= botEnemigo.vidaBarcos.Count; id++)
        {
            List<Vector2Int> coordenadas = new List<Vector2Int>();
            
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    if (botEnemigo.tableroEnemigo[x, y] == id)
                        coordenadas.Add(new Vector2Int(x, y));
                }
            }

            if (coordenadas.Count == 0) continue;

            // Ordenamos para saber si es horizontal o vertical
            coordenadas.Sort((a, b) => {
                if (a.x != b.x) return a.x.CompareTo(b.x);
                return a.y.CompareTo(b.y);
            });

            bool horizontal = (coordenadas[0].x == coordenadas[coordenadas.Count - 1].x);
            int indiceDiseño = Mathf.Clamp(id - 1, 0, diseñosFlota.Length - 1);
            DiseñoBarco disenoActual = diseñosFlota[indiceDiseño];

            for (int i = 0; i < coordenadas.Count; i++)
            {
                int px = coordenadas[i].x;
                int py = coordenadas[i].y;
                
                Image capaBarco = visualEnemigo[px, py].transform.GetChild(0).GetComponent<Image>();
                
                if (i == 0) capaBarco.sprite = disenoActual.proa;
                else if (i == coordenadas.Count - 1) capaBarco.sprite = disenoActual.popa;
                else capaBarco.sprite = disenoActual.cuerpo;

                // Lo hacemos invisible al inicio. ¡Solo aparecerá cuando le des un impacto!
                capaBarco.color = new Color(1, 1, 1, 0); 

                if (horizontal)
                    capaBarco.rectTransform.localRotation = Quaternion.Euler(0, 0, 270);
                else
                    capaBarco.rectTransform.localRotation = Quaternion.Euler(0, 0, 180);
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

    public void AlClicTableroEnemigo(int x, int y)
    {
        if (estadoActual != EstadoJuego.TurnoJugador) return;

        if (botEnemigo.tableroEnemigo[x, y] == -2 || botEnemigo.tableroEnemigo[x, y] == -1)
            return;
        
        string coordTexto = TraducirCoordenadas(x, y);
        int idImpacto = botEnemigo.tableroEnemigo[x, y];

        Image capaBarco = visualEnemigo[x, y].transform.GetChild(0).GetComponent<Image>();
        Image capaEfectos = visualEnemigo[x, y].transform.GetChild(1).GetComponent<Image>();

        if (idImpacto > 0) 
        {
            // Revelar esta pieza del barco enemigo haciéndolo visible
            capaBarco.color = Color.white; 
            
            // Iniciar ciclo de explosión y fuego infinito
            StartCoroutine(AnimarImpactoYFuego(capaEfectos));

            botEnemigo.tableroEnemigo[x, y] = -2; 
            botEnemigo.vidaBarcos[idImpacto]--;

            if (botEnemigo.vidaBarcos[idImpacto] <= 0)
            {
                string nombre = botEnemigo.nombresBarcos[idImpacto];
                labelResultados.text = $"¡Hundiste el {nombre} enemigo en {coordTexto}!";
                // Oscurecer ligeramente para indicar que está hundido
                capaBarco.color = new Color(0.6f, 0.6f, 0.6f); 
            }
            else
            {
                labelResultados.text = $"¡Tiro Certero! Impactaste un barco en {coordTexto}.";
            }
            
            aciertosJugador++;
            if (aciertosJugador >= impactosParaGanar)
            {
                MostrarPantallaFinal("¡VICTORIA ALMIRANTE!\nHundiste toda la flota enemiga.");
                return; 
            }
        }
        else if (idImpacto == 0) 
        {
            StartCoroutine(AnimarAgua(capaEfectos));
            botEnemigo.tableroEnemigo[x, y] = -1;    
            labelResultados.text = $"Fallaste. Disparo al agua en {coordTexto}.";
        }
        
        CambiarEstado(EstadoJuego.TurnoEnemigo);
    }

    private bool PuedeColocarBarco(int[,] tablero, int x, int y, int tamanio, bool horizontal)
    {
        for (int i = 0; i < tamanio; i++)
        {
            int nx = horizontal ? x : x + i;
            int ny = horizontal ? y + i : y;

            if (nx >= 10 || ny >= 10 || tablero[nx, ny] != 0) return false;
        }
        return true;
    }

    private void ColocarBarco(int[,] tablero, Image[,] visual, int x, int y, int tamanio, bool horizontal)
    {
        DiseñoBarco disenoActual = diseñosFlota[indiceBarcoActual];

        for (int i = 0; i < tamanio; i++)
        {
            int nx = horizontal ? x : x + i;
            int ny = horizontal ? y + i : y;

            tablero[nx, ny] = 1; 
            
            Image capaBarco = visual[nx, ny].transform.GetChild(0).GetComponent<Image>();
            
            if (i == 0) capaBarco.sprite = disenoActual.proa;
            else if (i == tamanio - 1) capaBarco.sprite = disenoActual.popa;
            else capaBarco.sprite = disenoActual.cuerpo;
            
            capaBarco.color = new Color(1, 1, 1, 1); 

            if (horizontal) capaBarco.rectTransform.localRotation = Quaternion.Euler(0, 0, 270);
            else capaBarco.rectTransform.localRotation = Quaternion.Euler(0, 0, 180);
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
        yield return new WaitForSeconds(1.5f);
        
        RecibirDisparoDelBot();

        if (estadoActual == EstadoJuego.FinJuego) yield break;

        CambiarEstado(EstadoJuego.TurnoJugador);
    }

    private void RecibirDisparoDelBot()
    {
        Vector2Int coordenadasAtaque = botEnemigo.TurnoDeAtaqueBot();
        int botX = coordenadasAtaque.x;
        int botY = coordenadasAtaque.y;

        string coordTexto = TraducirCoordenadas(botX, botY);

        Image capaBarco = visualJugador[botX, botY].transform.GetChild(0).GetComponent<Image>();
        Image capaEfectos = visualJugador[botX, botY].transform.GetChild(1).GetComponent<Image>();

        if (tableroJugador[botX, botY] == 1) 
        {
            StartCoroutine(AnimarImpactoYFuego(capaEfectos));
            capaBarco.color = new Color(0.6f, 0.6f, 0.6f);
            tableroJugador[botX, botY] = 2;

            labelResultados.text = $"¡Alerta! El enemigo impactó tu barco en {coordTexto}.";
            aciertosBot++;
            
            if (aciertosBot >= impactosParaGanar)
                MostrarPantallaFinal("¡DERROTA!\nEl enemigo destruyó tu flota.");
        }
        else
        {
            StartCoroutine(AnimarAgua(capaEfectos));
            tableroJugador[botX, botY] = 3;

            labelResultados.text = $"El enemigo disparó en {coordTexto} y cayó al agua.";
        }
    }

    private void ReiniciarPartida()
    {
        StopAllCoroutines(); // Muy importante detener los bucles de fuego
        
        tableroJugador = new int[10, 10];
        indiceBarcoActual = 0;
        esHorizontal = true;
        aciertosJugador = 0;
        aciertosBot = 0;
        nombreAlmirante = string.IsNullOrEmpty(inputNombreJugador.text) ? "Almirante" : inputNombreJugador.text;

        if (botEnemigo != null)
        {
            botEnemigo.GenerarFlotaEnemiga();
            botEnemigo.memoriaDisparosBot = new int[10, 10];
        }

        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                Image barcoJugador = visualJugador[x, y].transform.GetChild(0).GetComponent<Image>();
                Image efectoJugador = visualJugador[x, y].transform.GetChild(1).GetComponent<Image>();
                barcoJugador.sprite = null;
                barcoJugador.color = new Color(1, 1, 1, 0);
                efectoJugador.sprite = null;
                efectoJugador.color = new Color(1, 1, 1, 0);

                Image barcoEnemigo = visualEnemigo[x, y].transform.GetChild(0).GetComponent<Image>();
                Image efectoEnemigo = visualEnemigo[x, y].transform.GetChild(1).GetComponent<Image>();
                barcoEnemigo.sprite = null;
                barcoEnemigo.color = new Color(1, 1, 1, 0);
                efectoEnemigo.sprite = null;
                efectoEnemigo.color = new Color(1, 1, 1, 0);
            }
        }

        GenerarVisualesEnemigosOcultos(); // Mapeamos de nuevo la flota oculta

        if (panelFinJuego != null) panelFinJuego.SetActive(false);
        labelResultados.text = "Partida reiniciada. Nueva estrategia enemiga.";
        CambiarEstado(EstadoJuego.FaseColocacion);
    }

    private string TraducirCoordenadas(int fila, int columna)
    {
        char letra = (char)('A' + columna); 
        int numero = fila + 1;              
        return $"{letra}{numero}";
    }

    private void MostrarPantallaFinal(string mensaje)
    {
        estadoActual = EstadoJuego.FinJuego;
        labelEstado.text = "Partida finalizada";
        if (panelFinJuego != null) panelFinJuego.SetActive(true);
        if (textoFinJuego != null) textoFinJuego.text = mensaje;
    }

    public void ReiniciarJuego()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // --- CORRUTINAS DE ANIMACIÓN --- //

    private IEnumerator AnimarImpactoYFuego(Image capa)
    {
        capa.color = Color.white; 
        
        // 1. Restauramos el tamaño a 100% para que la explosión se vea en toda la casilla
        capa.rectTransform.localScale = Vector3.one; 

        // Efecto de explosión de inicio
        if (animacionExplosion != null && animacionExplosion.Length > 0)
        {
            foreach (Sprite frame in animacionExplosion)
            {
                capa.sprite = frame;
                yield return new WaitForSeconds(0.07f); 
            }
        }

        // 2. Reducimos el tamaño al 60% (0.6f) exclusivamente para el fuego
        capa.rectTransform.localScale = new Vector3(0.6f, 0.6f, 1f); 

        // Bucle infinito (El fuego no se apaga)
        if (animacionFuegoLoop != null && animacionFuegoLoop.Length > 0)
        {
            int frameActual = 0;
            while (true) 
            {
                capa.sprite = animacionFuegoLoop[frameActual];
                frameActual = (frameActual + 1) % animacionFuegoLoop.Length;
                yield return new WaitForSeconds(0.15f); 
            }
        }
    }

    private IEnumerator AnimarAgua(Image capa)
    {
        capa.color = Color.white;

        // 1. Restauramos el tamaño a 100% para que la salpicadura se vea grande
        capa.rectTransform.localScale = Vector3.one;

        // Reproducir la salpicadura
        if (animacionAgua != null && animacionAgua.Length > 0)
        {
            foreach (Sprite frame in animacionAgua)
            {
                capa.sprite = frame;
                yield return new WaitForSeconds(0.05f);
            }
        }

        // 2. Reducimos el tamaño al 45% (0.45f) para que el punto gris se vea como una pequeña marca
        capa.rectTransform.localScale = new Vector3(0.45f, 0.45f, 1f);

        // Dejar marca permanente
        if (spriteFallo != null)
        {
            capa.sprite = spriteFallo; 
        }
        else if (animacionAgua != null && animacionAgua.Length > 0)
        {
            capa.sprite = animacionAgua[animacionAgua.Length - 1];
            capa.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
        }
    }
}