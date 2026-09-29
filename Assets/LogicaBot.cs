using UnityEngine;

public class LogicaBot : MonoBehaviour
{
    // Matriz 10x10 que representa el tablero del bot. 
    // 0 = Agua, 1 = Barco
    public int[,] tableroEnemigo = new int[10, 10];
    
    // Tamaños clásicos de los barcos de Batalla Naval
    private int[] tamanosBarcos = { 5, 4, 3, 3, 2 };

    void Start()
    {
        GenerarFlotaEnemiga();
    }

    public void GenerarFlotaEnemiga()
    {
        // 1. Limpiar el tablero por si se reinicia el juego
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                tableroEnemigo[x, y] = 0;
            }
        }

        // 2. Intentar colocar cada barco de la lista
        foreach (int tamano in tamanosBarcos)
        {
            bool barcoColocado = false;

            while (!barcoColocado)
            {
                // Decidir rotación: true = horizontal, false = vertical
                bool esHorizontal = Random.Range(0, 2) == 0;
                
                // Elegir una coordenada inicial aleatoria (de 0 a 9)
                int fila = Random.Range(0, 10);
                int columna = Random.Range(0, 10);

                // Verificar si cabe y si el espacio está libre
                if (ValidarPosicionLibre(fila, columna, tamano, esHorizontal))
                {
                    // Registrar el barco en la matriz marcando con '1'
                    for (int i = 0; i < tamano; i++)
                    {
                        if (esHorizontal)
                            tableroEnemigo[fila, columna + i] = 1;
                        else
                            tableroEnemigo[fila + i, columna] = 1;
                    }
                    barcoColocado = true; 
                }
            }
        }
        
        Debug.Log("La flota enemiga ha tomado posiciones estratégicas.");
    }

    private bool ValidarPosicionLibre(int fila, int columna, int tamano, bool esHorizontal)
    {
        // Comprobación A: Límites del tablero
        // Si al sumar el tamaño del barco nos pasamos de 10, se sale del borde
        if (esHorizontal)
        {
            if (columna + tamano > 10) return false;
        }
        else
        {
            if (fila + tamano > 10) return false;
        }

        // Comprobación B: Colisiones con otros barcos ya colocados
        for (int i = 0; i < tamano; i++)
        {
            if (esHorizontal)
            {
                // Si la celda es distinta de 0, ya hay un barco ahí
                if (tableroEnemigo[fila, columna + i] != 0) return false; 
            }
            else
            {
                if (tableroEnemigo[fila + i, columna] != 0) return false; 
            }
        }

        // Si sobrevive a ambas comprobaciones, la posición es perfecta
        return true; 
    }
    // Matriz para recordar dónde ha disparado el bot (0 = no disparado, 1 = disparado)
    public int[,] memoriaDisparosBot = new int[10, 10];

    // Este método es llamado por la GUI para pedirle al bot sus coordenadas de ataque
    public Vector2Int TurnoDeAtaqueBot()
    {
        int fila, columna;
        
        // El bot busca coordenadas aleatorias hasta encontrar una donde NO haya disparado
        do
        {
            fila = Random.Range(0, 10);
            columna = Random.Range(0, 10);
        } while (memoriaDisparosBot[fila, columna] != 0);

        // Marca la coordenada en su memoria para no volver a elegirla
        memoriaDisparosBot[fila, columna] = 1;
        
        // Devuelve las coordenadas X (fila) y Y (columna)
        return new Vector2Int(fila, columna);
    }
}