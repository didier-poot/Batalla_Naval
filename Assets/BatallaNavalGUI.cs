using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class BatallaNavalGUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public TMP_Text labelEstado;
    public GameObject botonPrefab;
    public Transform gridParent;

    public enum EstadoJuego { FaseColocacion, TurnoJugador, TurnoEnemigo, FinJuego }
    private EstadoJuego estadoActual;

    private const int TAMANO = 10;
    private const int MAX_BARCOS = 5;

    private int[,] tableroLogico = new int[TAMANO, TAMANO];
    private Image[,] tableroVisual = new Image[TAMANO, TAMANO];

    private int barcosColocados = 0;
    private List<Vector2Int> barcosEnemigo = new List<Vector2Int>();
    private HashSet<Vector2Int> disparosEnemigo = new HashSet<Vector2Int>();
    private List<Vector2Int> objetivosEnemigo = new List<Vector2Int>();
    private bool reiniciando = false;

    void Start()
    {
        ReiniciarJuego();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ReiniciarJuego();
        }
    }

    private void ReiniciarJuego()
    {
        if (gridParent != null)
        {
            foreach (Transform hijo in gridParent)
            {
                Destroy(hijo.gameObject);
            }
        }

        reiniciando = false;
        barcosColocados = 0;
        barcosEnemigo.Clear();
        disparosEnemigo.Clear();
        objetivosEnemigo.Clear();
        tableroLogico = new int[TAMANO, TAMANO];
        tableroVisual = new Image[TAMANO, TAMANO];

        GenerarTablero();
        GenerarBarcosEnemigo();
        CambiarEstado(EstadoJuego.FaseColocacion);
    }

    void GenerarTablero()
    {
        for (int x = 0; x < TAMANO; x++)
        {
            for (int y = 0; y < TAMANO; y++)
            {
                tableroLogico[x, y] = 0;

                GameObject nuevoBoton = Instantiate(botonPrefab, gridParent);
                nuevoBoton.name = $"Casilla_{x}_{y}";
                tableroVisual[x, y] = nuevoBoton.GetComponent<Image>();

                int posX = x;
                int posY = y;
                nuevoBoton.GetComponent<Button>().onClick.AddListener(() => AlHacerClicEnCasilla(posX, posY));
            }
        }
    }

    public void AlHacerClicEnCasilla(int x, int y)
    {
        if (reiniciando) return;

        switch (estadoActual)
        {
            case EstadoJuego.FaseColocacion:
                ColocarBarco(x, y);
                break;

            case EstadoJuego.TurnoJugador:
                AtacarCasilla(x, y);
                break;

            case EstadoJuego.TurnoEnemigo:
            case EstadoJuego.FinJuego:
                break;
        }
    }

    private void ColocarBarco(int x, int y)
    {
        if (tableroLogico[x, y] == 0 && barcosColocados < MAX_BARCOS)
        {
            tableroLogico[x, y] = 1;
            barcosColocados++;

            ActualizarVisualCasilla(x, y);
            labelEstado.text = $"Coloca tus barcos ({barcosColocados}/{MAX_BARCOS})";

            if (barcosColocados >= MAX_BARCOS)
            {
                CambiarEstado(EstadoJuego.TurnoJugador);
            }
        }
    }

    private void AtacarCasilla(int x, int y)
    {
        if (x < 0 || x >= TAMANO || y < 0 || y >= TAMANO) return;

        if (tableroLogico[x, y] == 2 || tableroLogico[x, y] == 3) return;

        if (barcosEnemigo.Contains(new Vector2Int(x, y)))
        {
            tableroLogico[x, y] = 2;
            labelEstado.text = "¡Impacto!";

            if (ComprobarVictoriaEnemigo())
            {
                CambiarEstado(EstadoJuego.FinJuego);
                labelEstado.text = "¡Victoria! Has hundido todos los barcos enemigos. Pulsa R para reiniciar.";
                return;
            }
        }
        else
        {
            tableroLogico[x, y] = 3;
            labelEstado.text = "Agua... turno del enemigo.";
        }

        ActualizarVisualCasilla(x, y);
        CambiarEstado(EstadoJuego.TurnoEnemigo);
    }

    private IEnumerator SimularTurnoEnemigo()
    {
        yield return new WaitForSeconds(1.5f);

        if (estadoActual == EstadoJuego.FinJuego) yield break;

        Vector2Int ataque = ObtenerAtaqueEnemigo();
        if (ataque == new Vector2Int(-1, -1))
        {
            labelEstado.text = "No quedan más casillas. Pulsa R para reiniciar.";
            CambiarEstado(EstadoJuego.FinJuego);
            yield break;
        }

        disparosEnemigo.Add(ataque);

        if (tableroLogico[ataque.x, ataque.y] == 1)
        {
            tableroLogico[ataque.x, ataque.y] = 2;
            RegistrarObjetivoEnemigo(ataque);
            labelEstado.text = "¡Te han impactado!";

            if (ComprobarVictoriaJugador())
            {
                CambiarEstado(EstadoJuego.FinJuego);
                labelEstado.text = "Has perdido. Pulsa R para reiniciar.";
                yield break;
            }
        }
        else
        {
            tableroLogico[ataque.x, ataque.y] = 3;
            labelEstado.text = "El enemigo falló su tirada.";
        }

        ActualizarVisualCasilla(ataque.x, ataque.y);

        yield return new WaitForSeconds(0.8f);
        CambiarEstado(EstadoJuego.TurnoJugador);
    }

    private void RegistrarObjetivoEnemigo(Vector2Int posicion)
    {
        Vector2Int[] direcciones =
        {
            new Vector2Int(1,0),
            new Vector2Int(-1,0),
            new Vector2Int(0,1),
            new Vector2Int(0,-1)
        };

        foreach (Vector2Int dir in direcciones)
        {
            Vector2Int siguiente = posicion + dir;
            if (siguiente.x >= 0 && siguiente.x < TAMANO && siguiente.y >= 0 && siguiente.y < TAMANO)
            {
                if (!disparosEnemigo.Contains(siguiente) && !objetivosEnemigo.Contains(siguiente))
                {
                    objetivosEnemigo.Add(siguiente);
                }
            }
        }
    }

    private Vector2Int ObtenerAtaqueEnemigo()
    {
        if (objetivosEnemigo.Count > 0)
        {
            for (int i = objetivosEnemigo.Count - 1; i >= 0; i--)
            {
                Vector2Int objetivo = objetivosEnemigo[i];
                if (!disparosEnemigo.Contains(objetivo))
                {
                    objetivosEnemigo.RemoveAt(i);
                    return objetivo;
                }
            }

            objetivosEnemigo.Clear();
        }

        List<Vector2Int> disponibles = new List<Vector2Int>();
        for (int x = 0; x < TAMANO; x++)
        {
            for (int y = 0; y < TAMANO; y++)
            {
                if (tableroLogico[x, y] != 2 && tableroLogico[x, y] != 3 && !disparosEnemigo.Contains(new Vector2Int(x, y)))
                {
                    disponibles.Add(new Vector2Int(x, y));
                }
            }
        }

        if (disponibles.Count == 0) return new Vector2Int(-1, -1);
        return disponibles[Random.Range(0, disponibles.Count)];
    }

    private bool ComprobarVictoriaEnemigo()
    {
        foreach (Vector2Int posicion in barcosEnemigo)
        {
            if (tableroLogico[posicion.x, posicion.y] == 1)
            {
                return false;
            }
        }

        return true;
    }

    private bool ComprobarVictoriaJugador()
    {
        for (int x = 0; x < TAMANO; x++)
        {
            for (int y = 0; y < TAMANO; y++)
            {
                if (tableroLogico[x, y] == 1)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void GenerarBarcosEnemigo()
    {
        barcosEnemigo.Clear();
        int intentos = 0;

        while (barcosEnemigo.Count < MAX_BARCOS && intentos < 200)
        {
            int x = Random.Range(0, TAMANO);
            int y = Random.Range(0, TAMANO);

            if (!barcosEnemigo.Contains(new Vector2Int(x, y)))
            {
                barcosEnemigo.Add(new Vector2Int(x, y));
            }

            intentos++;
        }
    }

    private void ActualizarVisualCasilla(int x, int y)
    {
        int estadoLogico = tableroLogico[x, y];
        Image imagenBoton = tableroVisual[x, y];

        switch (estadoLogico)
        {
            case 0:
                imagenBoton.color = Color.white;
                break;
            case 1:
                imagenBoton.color = Color.gray;
                break;
            case 2:
                imagenBoton.color = Color.red;
                break;
            case 3:
                imagenBoton.color = Color.cyan;
                break;
        }
    }

    private void CambiarEstado(EstadoJuego nuevoEstado)
    {
        estadoActual = nuevoEstado;

        switch (estadoActual)
        {
            case EstadoJuego.FaseColocacion:
                labelEstado.text = $"Coloca tus barcos (0/{MAX_BARCOS})";
                break;

            case EstadoJuego.TurnoJugador:
                labelEstado.text = "¡Ataca al enemigo!";
                break;

            case EstadoJuego.TurnoEnemigo:
                labelEstado.text = "Turno del enemigo...";
                reiniciando = false;
                StartCoroutine(SimularTurnoEnemigo());
                break;

            case EstadoJuego.FinJuego:
                reiniciando = true;
                break;
        }
    }
}