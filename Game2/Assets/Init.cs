using System;
using UnityEngine;
using UnityEngine.UI;
using Model;


public class Init : MonoBehaviour
{
    public static Init Instance;
    public ILRuntime.Runtime.Enviorment.AppDomain AppDomain;

    public InputField pname;
    public InputField password;
    public Button loginBut;
    public Button enterMap;

    GameObject uiLogin;
    GameObject uiLobby;

    private async void Start()
    {
        try
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;

            ObjectEvents.Instance.Add("Model", typeof(Game).Assembly);

            //Game.Scene.AddComponent<GlobalConfigComponent>();
            Game.Scene.AddComponent<OpcodeTypeComponent>();
            Game.Scene.AddComponent<NetOuterComponent>();
            Game.Scene.AddComponent<ResourcesComponent>();
            //Game.Scene.AddComponent<BehaviorTreeComponent>();
            Game.Scene.AddComponent<PlayerComponent>();
            Game.Scene.AddComponent<UnitComponent>();
            Game.Scene.AddComponent<ClientFrameComponent>();

            await BundleHelper.DownloadBundle();

            // 加载配置
            Game.Scene.GetComponent<ResourcesComponent>().LoadBundle("config.unity3d");
            Game.Scene.AddComponent<ConfigComponent>();
            Game.Scene.GetComponent<ResourcesComponent>().UnloadBundle("config.unity3d");

            //MessageDispatherComponent
            Game.Scene.AddComponent<MessageDispatherComponent>();


            uiLogin = GameObject.Find("UILogin");
            uiLobby = GameObject.Find("UILobby");
            uiLobby.SetActive(false);

            loginBut.onClick.AddListener(OnLogin);
            enterMap.onClick.AddListener(EnterMap);

            //this.start.Run();
            Game.Scene.GetComponent<EventComponent>().Run(EventIdType.InitSceneStart);
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }
    }



    public void OnLogin()
    {
        Log.Info("click login!");
        Session session = null;
        session = Game.Scene.GetComponent<NetOuterComponent>().Create(NetworkHelper.ToIPEndPoint("127.0.0.1:10002"));
        string text = pname.text;

        session.CallWithAction(new C2R_Login() { Account = text, Password = "111111" }, (response) => LoginOK(response));
    }
    private void LoginOK(AResponse response)
    {
        R2C_Login r2CLogin = (R2C_Login)response;
        if (r2CLogin.Error != ErrorCode.ERR_Success)
        {
            Log.Error(r2CLogin.Error.ToString());
            return;
        }

        Session gateSession = Game.Scene.GetComponent<NetOuterComponent>().Create(NetworkHelper.ToIPEndPoint(r2CLogin.Address));
        Game.Scene.AddComponent<SessionComponent>().Session = gateSession;

        SessionComponent.Instance.Session.CallWithAction(new C2G_LoginGate() { Key = r2CLogin.Key },
                 (response2) => LoginGateOk(response2));

    }
    private void LoginGateOk(AResponse response)
    {
        G2C_LoginGate g2CLoginGate = (G2C_LoginGate)response;
        if (g2CLoginGate.Error != ErrorCode.ERR_Success)
        {
            Log.Error(g2CLoginGate.Error.ToString());
            return;
        }

        uiLogin.SetActive(false);
        uiLobby.SetActive(true);
        Log.Info("登陆gate成功!");

        // 创建Player
        Player player = Model.EntityFactory.CreateWithId<Player>(g2CLoginGate.PlayerId);
        PlayerComponent playerComponent = Game.Scene.GetComponent<PlayerComponent>();
        playerComponent.MyPlayer = player;
    }

    private async void EnterMap()
    {
        try
        {
            Debug.Log(SessionComponent.Instance.Session);
            G2C_EnterMap g2CEnterMap = await SessionComponent.Instance.Session.Call<G2C_EnterMap>(new C2G_EnterMap());


            uiLobby.SetActive(false);
            Log.Info("EnterMap...");
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }
    }


    private void Update()
    {
        //this.update?.Run();
        ObjectEvents.Instance.Update();
    }

    private void LateUpdate()
    {
        //this.lateUpdate?.Run();
        ObjectEvents.Instance.LateUpdate();
    }

    private void OnApplicationQuit()
    {
        Instance = null;
        Game.Close();
        ObjectEvents.Close();
        //this.onApplicationQuit?.Run();
    }
}
