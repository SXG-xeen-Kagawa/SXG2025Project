using UnityEngine;


namespace SXG2025
{

    public class GameDataHolder : MonoBehaviour
    {
        private static GameDataHolder ms_instance = null;

        internal static GameDataHolder Instance => ms_instance;


        private DataFormatTank m_dataTank = null;
        internal DataFormatTank DataTank => m_dataTank;


        private DataFormatGame m_dataGame = null;
        internal DataFormatGame DataGame => m_dataGame;


        private void Awake()
        {
            if (ms_instance == null)
            {
                ms_instance = this;
            } else
            {
                Destroy(this.gameObject);
                return;
            }
            DontDestroyOnLoad(this.gameObject);

            // データロード 
            LoadData();
        }


        private void LoadData()
        {
            m_dataTank = Resources.Load<DataFormatTank>("DataTank");
            m_dataGame = Resources.Load<DataFormatGame>("DataGame");
        }



        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            if (Instance == null)
            {
                GameObject obj = new GameObject("GameDataHolder");
                obj.AddComponent<GameDataHolder>();
            }
        }

    }


}

