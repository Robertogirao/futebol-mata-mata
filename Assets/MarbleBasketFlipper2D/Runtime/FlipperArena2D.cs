using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MarbleBasketFlipper2D
{
    public enum LavaRule2D { VoltaAoFunil, EliminadaNaPartida }
    // Append new values so a theme already saved in an existing scene remains the same.
    public enum TemaArena2D
    {
        Fogo, Agua, Eletricidade, Selva, Vento, Gelo, Caverna,
        [InspectorName("Templo Celestial")] TemploCelestial,
        [InspectorName("Ruínas Antigas")] RuinasAntigas,
        [InspectorName("Pântano Venenoso")] PantanoVenenoso,
        Deserto,
        [InspectorName("Galáxia")] Galaxia,
        [InspectorName("Cidade Neon")] CidadeNeon,
        [InspectorName("Mundo dos Doces")] MundoDosDoces,
        Piratas, Halloween
    }

    /// <summary>Independent 2D funnel/flipper match with two scoring hoops.</summary>
    [DefaultExecutionOrder(-500)]
    public sealed class FlipperArena2D : MonoBehaviour
    {
        [Header("Partida")]
        [Range(1, 6)] public int bolasPorTime = 5;
        [Min(10)] public float duracaoSegundos = 120;
        public string nomeVermelho = "VERMELHO";
        public string nomeAzul = "AZUL";
        [InspectorName("Regra do elemento")]
        [Tooltip("Regra para uma bolinha que erra e cai no elemento ou sai da arena. Uma cesta volta ao funil em ambos os modos.")]
        public LavaRule2D regraDaLava = LavaRule2D.VoltaAoFunil;
        [Tooltip("Variação adicional do impulso, igual para as duas equipes.")]
        [Range(0, 1)] public float variacaoArremesso = .8f;
        [Tooltip("Velocidade média dos movimentos completos de ida e volta da alavanca, por segundo.")]
        [Range(.5f, 2f)] public float velocidadeDaAlavanca = 1.5f;
        [Tooltip("Velocidade com que o ponto de nascimento se move para os lados dentro do funil.")]
        [Range(.2f, 3f)] public float velocidadeDoNascimento = 1.1f;
        [Tooltip("Velocidade da barra que sobe e desce diante das cestas, em ciclos por segundo.")]
        [Range(.15f, .9f)] public float velocidadeDaBarreira = .35f;
        [Tooltip("Move suavemente as duas cestas e ajusta a altura dos suportes.")]
        public bool moverCestas = true;
        [Header("Tema da arena")]
        [Tooltip("Escolha antes de apertar Play: muda o fundo, o elemento dos buracos e o efeito do contato.")]
        public TemaArena2D temaDaArena = TemaArena2D.Fogo;

        public int RedScore { get; private set; }
        public int BlueScore { get; private set; }
        public float RemainingSeconds { get; private set; }
        public bool Finished { get; private set; }
        public string RedName => string.IsNullOrWhiteSpace(nomeVermelho) ? "VERMELHO" : nomeVermelho;
        public string BlueName => string.IsNullOrWhiteSpace(nomeAzul) ? "AZUL" : nomeAzul;
        public Camera Camera { get; private set; }
        internal Vector2 BasketVisualOffset(int points)
        {
            BasketRig basket = points == 3 ? highBasket : lowBasket;
            return basket != null && basket.body != null ? basket.Offset : Vector2.zero;
        }

        private enum Phase { InChute, Flight, Scored, Burning, Returning, Eliminated }
        private sealed class Marble
        {
            public int team, scoredHoop, activeNet;
            public Rigidbody2D body;
            public CircleCollider2D collider;
            public GameObject visual;
            public Phase phase;
            public float age, stagnantTime, floorAge;
            public bool ignoringExit, scoredBeforeBurn;
            public Vector2 previous, progressAnchor, burnOrigin;
            public SpriteRenderer ballSprite, reflection, fireOuter, fireCore;
            public Transform elementRoot;
            public SpriteRenderer[] elementParts;
        }
        private sealed class BasketNet
        {
            public Rigidbody2D body;
            public Collider2D left, right;
            public float angle, angularVelocity;
        }
        private sealed class BasketRig
        {
            public int points;
            public float left, right, y, supportHeight;
            public Rigidbody2D body;
            public BasketNet net;
            public Transform baseRoot, supportRoot;
            public Vector2 netOrigin, previousOffset;
            public Vector2 Offset => body.transform.localPosition;
        }

        private const float BallRadius = .17f;
        private const float Gravity = 9.81f;
        private const float RestAngle = (float)FlipperMotion2D.MinimumAngle;
        private const float LowY = FlipperRules2D.LowY, LowLeft = FlipperRules2D.LowLeft, LowRight = FlipperRules2D.LowRight;
        private const float HighY = FlipperRules2D.HighY, HighLeft = FlipperRules2D.HighLeft, HighRight = FlipperRules2D.HighRight;
        private static readonly Vector2 Pivot = new Vector2(-6.00f, -3.08f);
        private static readonly Color Red = new Color(.98f, .035f, .06f);
        private static readonly Color Blue = new Color(.04f, .30f, 1f);
        private static readonly Color Steel = new Color(.55f, .68f, .86f);
        private static readonly Color Neon = new Color(.18f, .78f, 1f);
        private static readonly Color Dark = new Color(.035f, .055f, .10f);
        private readonly List<Marble> balls = new List<Marble>();
        private readonly Dictionary<Rigidbody2D, Marble> ballLookup = new Dictionary<Rigidbody2D, Marble>();
        private readonly Dictionary<Marble, float> pendingImpacts = new Dictionary<Marble, float>();
        private readonly List<UnityEngine.Object> assets = new List<UnityEngine.Object>();
        private readonly System.Random random = new System.Random();
        private Transform world;
        private FlipperArt2D art;
        private Rigidbody2D flipper, obstacle;
        private Collider2D flipperCollider;
        private PhysicsMaterial2D tracks, ballMaterial, netMaterial;
        private BasketNet lowNet, highNet;
        private BasketRig lowBasket, highBasket;
        private TemaArena2D activeTheme;
        private float elapsed, flipperAngle = RestAngle, flipperAngularVelocity;
        private RenderPipelineAsset oldGraphics, oldQuality;
        private bool pipelineAcquired;

        private void Awake()
        {
            activeTheme = Enum.IsDefined(typeof(TemaArena2D), temaDaArena) ? temaDaArena : TemaArena2D.Fogo;
            bolasPorTime = Mathf.Clamp(bolasPorTime, 1, 6);
            duracaoSegundos = Mathf.Max(10, duracaoSegundos);
            variacaoArremesso = Mathf.Clamp01(variacaoArremesso);
            velocidadeDoNascimento = velocidadeDoNascimento <= 0 ? 1.1f : Mathf.Clamp(velocidadeDoNascimento, .2f, 3f);
            velocidadeDaAlavanca = velocidadeDaAlavanca <= 0 ? 1.5f : Mathf.Clamp(velocidadeDaAlavanca, .5f, 2f);
            velocidadeDaBarreira = velocidadeDaBarreira <= 0 ? .35f : Mathf.Clamp(velocidadeDaBarreira, .15f, .9f);
            RemainingSeconds = duracaoSegundos;
            oldGraphics = GraphicsSettings.defaultRenderPipeline; oldQuality = QualitySettings.renderPipeline;
            pipelineAcquired = true; QualitySettings.renderPipeline = null; GraphicsSettings.defaultRenderPipeline = null;
            try { Build(); }
            catch (Exception error)
            {
                Debug.LogException(error, this); RestorePipeline();
                if (world != null) world.gameObject.SetActive(false);
                enabled = false;
            }
        }
        private void Build()
        {
            world = Node("Arena de duas cestas gerada", transform);
            art = new FlipperArt2D(world); BuildCamera(); art.Background(activeTheme);
            tracks = Material("Trilhos do funil", .025f, .25f);
            ballMaterial = Material("Bolinha do funil", .035f, .40f);
            netMaterial = Material("Corda da rede", .12f, .05f);
            BuildChute(); BuildFlipper(); BuildFloor(); BuildObstacle();
            lowBasket = BuildBasket("Cesta de 2", LowLeft, LowRight, LowY, 2);
            highBasket = BuildBasket("Cesta de 3", HighLeft, HighRight, HighY, 3);
            lowNet = lowBasket.net; highNet = highBasket.net;
            for (int i = 0; i < bolasPorTime; i++) { NewBall(-1, i); NewBall(1, i); }
            var positions = SpawnField2D.InitialPositions(balls.Count, BallRadius, random);
            for (int i = 0; i < balls.Count; i++) Spawn(balls[i], positions[i]);
            var hud = gameObject.AddComponent<FlipperHud2D>(); hud.arena = this; hud.panel = art.PanelTexture;
        }
        private static Transform Node(string name, Transform parent)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); return t;
        }
        private PhysicsMaterial2D Material(string name, float friction, float bounce)
        {
            var m = new PhysicsMaterial2D(name) { friction = friction, bounciness = bounce }; assets.Add(m); return m;
        }
        private void BuildCamera()
        {
            var go = Node("Câmera 2D", world).gameObject; go.tag = "MainCamera";
            go.transform.position = new Vector3(0, 0, -10);
            Camera = go.AddComponent<Camera>(); Camera.orthographic = true; Camera.orthographicSize = 4.5f;
            Camera.clearFlags = CameraClearFlags.SolidColor; Camera.backgroundColor = new Color(.015f, .02f, .06f);
            Camera.nearClipPlane = .1f; Camera.farClipPlane = 30; Camera.allowHDR = false; Camera.allowMSAA = true;
        }
        private void LateUpdate()
        {
            if (Camera == null || Screen.height <= 0) return;
            float window = (float)Screen.width / Screen.height, desired = 16f / 9;
            Camera.rect = window > desired ? new Rect((1 - desired / window) * .5f, 0, desired / window, 1)
                : new Rect(0, (1 - window / desired) * .5f, 1, window / desired);
            Camera.aspect = desired;
        }
        private void BuildChute()
        {
            Vector2 leftTop = new Vector2((float)SpawnField2D.LeftTop, (float)SpawnField2D.TopY), rightTop = new Vector2((float)SpawnField2D.RightTop, (float)SpawnField2D.TopY);
            Vector2 leftNeck = new Vector2((float)SpawnField2D.LeftNeck, (float)SpawnField2D.NeckY), rightNeck = new Vector2((float)SpawnField2D.RightNeck, (float)SpawnField2D.NeckY);
            Rail("Funil esquerdo", leftTop, leftNeck, .14f);
            Rail("Funil direito", rightTop, rightNeck, .14f);
            Rail("Parede esquerda", leftNeck, new Vector2(-6.24f, -2.65f), .10f);
            // The vertical right wall is absent, leaving the requested open side.
            Rail("Curva inferior 1", new Vector2(-6.24f, -2.65f), new Vector2(-6.13f, -2.90f), .10f);
            Rail("Curva inferior 2", new Vector2(-6.13f, -2.90f), new Vector2(-6.00f, -3.12f), .10f);
        }
        private void BuildFlipper()
        {
            ConveyorSupport();
            var pivot = Node("Lançador móvel", world); pivot.localPosition = Pivot;
            flipper = pivot.gameObject.AddComponent<Rigidbody2D>(); flipper.bodyType = RigidbodyType2D.Kinematic;
            flipper.rotation = RestAngle;
            // The open funnel leaves more balls to the right, so extend the active paddle to catch them.
            var blade = art.Box("Alavanca móvel", new Vector2(1.20f, 0), new Vector2(2.4f, .24f), Steel, 3, pivot);
            var collider = blade.gameObject.AddComponent<BoxCollider2D>(); collider.size = new Vector2(.94f, .80f); collider.sharedMaterial = tracks;
            flipperCollider = collider;
            var relay = pivot.gameObject.AddComponent<FlipperImpactRelay2D>(); relay.arena = this;
        }
        private void ConveyorSupport()
        {
            // A small seesaw-like visual support; it adds no physics or collision.
            Vector2 top = Pivot + new Vector2(0, -.16f);
            Vector2 left = new Vector2(Pivot.x - .34f, -3.72f), right = new Vector2(Pivot.x + .34f, -3.72f);
            art.Line("Apoio da esteira · esquerda", left, top, .11f, Dark, -4);
            art.Line("Apoio da esteira · direita", top, right, .11f, Dark, -4);
            art.Line("Apoio da esteira · esquerda clara", left, top, .05f, Steel, -3);
            art.Line("Apoio da esteira · direita clara", top, right, .05f, Steel, -3);
            art.Line("Apoio da esteira · base", left, right, .055f, Neon, -3);
        }
        private void BuildFloor()
        {
            // Visible floor and lava are already part of the background artwork.
            // Add physical rails only to the three solid stretches.
            float y = (float)ArenaSurface2D.FloorY - .05f;
            PhysicalRail("Chão esquerdo", new Vector2((float)ArenaSurface2D.LeftEdge, y), new Vector2((float)ArenaSurface2D.LavaOneLeft, y), .10f, tracks);
            PhysicalRail("Chão central", new Vector2((float)ArenaSurface2D.LavaOneRight, y), new Vector2((float)ArenaSurface2D.LavaTwoLeft, y), .10f, tracks);
            PhysicalRail("Chão direito", new Vector2((float)ArenaSurface2D.LavaTwoRight, y), new Vector2((float)ArenaSurface2D.RightEdge, y), .10f, tracks);
        }
        private void BuildObstacle()
        {
            var t = Node("Barreira móvel", world);
            t.localPosition = new Vector2((float)ArenaSurface2D.ObstacleX, (float)ArenaSurface2D.ObstacleY(0, velocidadeDaBarreira));
            obstacle = t.gameObject.AddComponent<Rigidbody2D>(); obstacle.bodyType = RigidbodyType2D.Kinematic;
            var frame = art.Box("Barra · moldura", Vector2.zero, new Vector2(.24f, 1.05f), Steel, 4, t);
            var collider = frame.gameObject.AddComponent<BoxCollider2D>(); collider.size = new Vector2(.90f, .96f); collider.sharedMaterial = tracks;
            art.Box("Barra · neon", Vector2.zero, new Vector2(.14f, .95f), Neon, 5, t);
            art.Box("Barra · brilho", Vector2.zero, new Vector2(.055f, .85f), Color.white, 6, t);
        }
        private BasketRig BuildBasket(string name, float left, float right, float y, int points)
        {
            float postX = right + .17f;
            var moving = Node(name + " · estrutura móvel", world);
            var hoopBody = moving.gameObject.AddComponent<Rigidbody2D>(); hoopBody.bodyType = RigidbodyType2D.Kinematic;
            var baseRoot = Node(name + " · base móvel", world);
            var supportRoot = Node(name + " · suporte telescópico", baseRoot);
            const float footY = -3.72f;
            supportRoot.localPosition = new Vector2(postX + .12f, footY);
            BasketStructure(name, postX, right, y, moving, baseRoot, supportRoot);
            PhysicalRail(name + " · tabela física", new Vector2(postX - .05f, y), new Vector2(postX - .05f, y + 1.05f), .08f, tracks, moving);
            CirclePost(name + " · borda esquerda", new Vector2(left, y), moving);
            CirclePost(name + " · borda direita", new Vector2(right, y), moving);
            var netRoot = Node(name + " · rede móvel", world);
            netRoot.localPosition = new Vector2((left + right) * .5f, art.BasketNetTopY(y));
            var net = new BasketNet { body = netRoot.gameObject.AddComponent<Rigidbody2D>() };
            net.body.bodyType = RigidbodyType2D.Kinematic;
            float half = (right - left) * .5f;
            net.left = NetRail(netRoot, "Corda esquerda", new Vector2(-half + .13f, -.12f), new Vector2(-.27f, -.57f));
            net.right = NetRail(netRoot, "Corda direita", new Vector2(half - .13f, -.12f), new Vector2(.27f, -.57f));
            var relay = netRoot.gameObject.AddComponent<NetContactRelay2D>(); relay.arena = this; relay.points = points;
            if (!art.BasketHoop(name, left, right, y, netRoot, moving))
            {
                // Fallback keeps the basket visible if a texture was not imported.
                art.Line(name + " · aro externo", new Vector2(left, y), new Vector2(right, y), .13f, new Color(1f, .10f, .12f), 5, moving);
                art.Line(name + " · brilho", new Vector2(left, y + .02f), new Vector2(right, y + .02f), .035f, Color.white, 6, moving);
                for (int i = 0; i <= 6; i++)
                {
                    float top = Mathf.Lerp(-half, half, i / 6f), bottom = Mathf.Lerp(-.33f, .33f, i / 6f);
                    art.Line(name + " · rede", new Vector2(top, 0), new Vector2(bottom, -.57f), .017f, Color.white, 5, netRoot);
                }
                art.Line(name + " · fundo da rede", new Vector2(-.33f, -.57f), new Vector2(.33f, -.57f), .025f, Color.white, 5, netRoot);
            }
            return new BasketRig { points = points, left = left, right = right, y = y,
                body = hoopBody, net = net, baseRoot = baseRoot, supportRoot = supportRoot,
                supportHeight = y + .13f - footY, netOrigin = netRoot.localPosition };
        }
        private Collider2D NetRail(Transform parent, string name, Vector2 a, Vector2 b)
        {
            var t = Node(name, parent); Vector2 d = b - a;
            t.localPosition = (a + b) * .5f; t.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            var c = t.gameObject.AddComponent<BoxCollider2D>(); c.size = new Vector2(d.magnitude, .04f); c.sharedMaterial = netMaterial; return c;
        }
        private void BasketStructure(string name, float postX, float right, float y,
            Transform moving, Transform baseRoot, Transform supportRoot)
        {
            // The foot stays on the floor while the decorative post extends with the hoop.
            float poleX = postX + .12f, footY = -3.72f;
            Vector2 bottom = Vector2.zero, top = new Vector2(0, y + .13f - footY);
            if (!art.GuideRail(name + " · poste metálico", bottom, top, .25f, -4, supportRoot))
                art.Line(name + " · poste", bottom, top, .19f, Steel, -4, supportRoot);
            art.Box(name + " · base sombra", new Vector2(poleX, footY), new Vector2(.66f, .18f), Dark, -3, baseRoot);
            art.Box(name + " · base aço", new Vector2(poleX, footY + .025f), new Vector2(.56f, .105f), Steel, -2, baseRoot);
            art.Line(name + " · base luz", new Vector2(poleX - .22f, footY + .055f), new Vector2(poleX + .22f, footY + .055f), .025f, Neon, -1, baseRoot);
            art.Disk(name + " · base parafuso esquerdo", new Vector2(poleX - .23f, footY + .01f), .045f, Dark, 0, baseRoot);
            art.Disk(name + " · base parafuso direito", new Vector2(poleX + .23f, footY + .01f), .045f, Dark, 0, baseRoot);

            float boardX = postX + .11f;
            art.Box(name + " · moldura da tabela", new Vector2(boardX, y + .55f), new Vector2(.32f, 1.18f), Steel, 0, moving);
            art.Box(name + " · luz da tabela", new Vector2(boardX, y + .55f), new Vector2(.26f, 1.11f), Neon, 1, moving);
            art.Box(name + " · face da tabela", new Vector2(boardX, y + .55f), new Vector2(.19f, 1.04f), new Color(.88f, .94f, 1f), 2, moving);
            art.Box(name + " · faixa da tabela", new Vector2(boardX + .025f, y + .55f), new Vector2(.045f, .70f), new Color(.80f, .04f, .09f), 3, moving);
            art.Line(name + " · reflexo da tabela", new Vector2(boardX - .045f, y + .10f), new Vector2(boardX - .045f, y + 1.00f), .025f, Color.white, 3, moving);
            art.Disk(name + " · fixação superior", new Vector2(boardX, y + 1.05f), .055f, Dark, 4, moving);
            art.Disk(name + " · fixação inferior", new Vector2(boardX, y + .05f), .055f, Dark, 4, moving);
            art.Line(name + " · encaixe do aro", new Vector2(right, y), new Vector2(postX + .02f, y), .10f, Steel, 4, moving);
        }
        private void CirclePost(string name, Vector2 position, Transform parent = null)
        {
            var t = Node(name, parent != null ? parent : world); t.localPosition = position;
            var c = t.gameObject.AddComponent<CircleCollider2D>(); c.radius = .06f; c.sharedMaterial = tracks;
        }
        private void VisualRail(string name, Vector2 a, Vector2 b, float thickness)
        {
            if (art.GuideRail(name + " · trilho ilustrado", a, b, thickness + .12f, 2)) return;
            art.Line(name + " · sombra", a, b, thickness + .18f, Dark, 0);
            art.Line(name + " · neon", a, b, thickness + .06f, new Color(.04f, .50f, 1f), 1);
            art.Line(name + " · aço", a, b, thickness, Steel, 2);
            art.Line(name + " · reflexo", a, b, .025f, Color.white, 3);
        }
        private Collider2D Rail(string name, Vector2 a, Vector2 b, float thickness)
        {
            VisualRail(name, a, b, thickness); return PhysicalRail(name, a, b, thickness, tracks);
        }
        private Collider2D PhysicalRail(string name, Vector2 a, Vector2 b, float thickness, PhysicsMaterial2D material, Transform parent = null)
        {
            var t = Node(name + " · colisão", parent != null ? parent : world); Vector2 d = b - a;
            t.localPosition = (a + b) * .5f; t.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            var c = t.gameObject.AddComponent<BoxCollider2D>(); c.size = new Vector2(d.magnitude, thickness); c.sharedMaterial = material; return c;
        }
        private void NewBall(int team, int index)
        {
            var t = Node((team < 0 ? "Vermelho " : "Azul ") + (index + 1), world);
            var body = t.gameObject.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0; body.collisionDetectionMode = CollisionDetectionMode2D.Continuous; body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collider = t.gameObject.AddComponent<CircleCollider2D>(); collider.radius = BallRadius; collider.sharedMaterial = ballMaterial; collider.enabled = false;
            var ballSprite = art.Marble("Bolinha", Vector2.zero, BallRadius * 2, team < 0 ? Red : Blue, 10, t);
            var reflection = art.Disk("Reflexo", new Vector2(-.06f, .065f), .055f, new Color(1, 1, 1, .9f), 11, t);
            var fireOuter = art.Disk("Chama externa", new Vector2(0, .11f), .35f, new Color(1, .24f, .025f, .85f), 12, t);
            var fireCore = art.Disk("Chama interna", new Vector2(0, .11f), .20f, new Color(1, .84f, .18f, .95f), 13, t);
            fireOuter.enabled = false; fireCore.enabled = false;
            var elementRoot = Node("Efeito do elemento", t);
            var elementParts = BuildElementEffect(elementRoot);
            var ball = new Marble { team = team, body = body, collider = collider, visual = t.gameObject,
                ballSprite = ballSprite, reflection = reflection, fireOuter = fireOuter, fireCore = fireCore,
                elementRoot = elementRoot, elementParts = elementParts, phase = Phase.Returning };
            balls.Add(ball); ballLookup.Add(body, ball);
        }
        private SpriteRenderer[] BuildElementEffect(Transform parent)
        {
            var parts = new List<SpriteRenderer>();
            if (activeTheme == TemaArena2D.Agua)
            {
                parts.Add(art.Ring("Água · onda", new Vector2(0, -.12f), new Vector2(.42f, .11f), new Color(.37f, .90f, 1f), 12, parent));
                for (int i = 0; i < 7; i++)
                    parts.Add(art.Disk("Água · gota " + (i + 1), Vector2.zero, .07f, new Color(.55f, .94f, 1f), 13, parent));
            }
            else if (activeTheme == TemaArena2D.Eletricidade)
            {
                parts.Add(art.Disk("Eletricidade · clarão", Vector2.zero, .55f, new Color(.30f, .54f, 1f, .38f), 9, parent));
                for (int side = -1; side <= 1; side += 2)
                {
                    var points = new[] { new Vector2(side * .14f, -.14f), new Vector2(side * .32f, -.03f),
                        new Vector2(side * .22f, .10f), new Vector2(side * .43f, .24f), new Vector2(side * .29f, .40f) };
                    for (int i = 0; i < points.Length - 1; i++)
                        parts.Add(art.Line("Eletricidade · faísca " + side + "/" + i, points[i], points[i + 1],
                            .035f, new Color(.70f, .91f, 1f), 13, parent));
                }
            }
            else if (activeTheme == TemaArena2D.Selva)
            {
                parts.Add(art.Ring("Selva · brilho", new Vector2(0, -.12f), new Vector2(.42f, .11f), new Color(.50f, .90f, .30f), 12, parent));
                for (int i = 0; i < 7; i++)
                    parts.Add(art.Leaf("Selva · folha " + (i + 1), Vector2.zero, new Vector2(.08f, .14f),
                        new Color(.34f, .88f, .22f), 13, parent));
            }
            else if (activeTheme == TemaArena2D.Vento)
            {
                for (int i = 0; i < 4; i++)
                    parts.Add(art.Ring("Vento · redemoinho " + (i + 1), Vector2.zero,
                        new Vector2(.42f, .12f), new Color(.84f, .95f, 1f), 12, parent));
                for (int i = 0; i < 4; i++)
                    parts.Add(art.Line("Vento · rastro " + (i + 1), new Vector2(-.18f, 0), new Vector2(.18f, 0),
                        .022f, new Color(.90f, .98f, 1f), 13, parent));
            }
            else if (activeTheme == TemaArena2D.Gelo)
            {
                parts.Add(art.Disk("Gelo · brilho", Vector2.zero, .48f, new Color(.42f, .85f, 1f, .32f), 9, parent));
                for (int i = 0; i < 7; i++)
                    parts.Add(art.Crystal("Gelo · cristal " + (i + 1), Vector2.zero, new Vector2(.06f, .14f),
                        new Color(.70f, .95f, 1f), 13, parent));
            }
            else if (activeTheme == TemaArena2D.Caverna)
            {
                parts.Add(art.Ring("Caverna · poeira", new Vector2(0, -.12f), new Vector2(.42f, .11f), new Color(.69f, .60f, .85f), 12, parent));
                for (int i = 0; i < 7; i++)
                    parts.Add(art.Crystal("Caverna · fragmento " + (i + 1), Vector2.zero, new Vector2(.08f, .11f),
                        new Color(.72f, .64f, .88f), 13, parent));
            }
            else if (activeTheme == TemaArena2D.TemploCelestial)
            {
                parts.Add(art.Ring("Templo · auréola", new Vector2(0, -.12f), new Vector2(.42f, .11f), new Color(1f, .88f, .57f), 12, parent));
                for (int i = 0; i < 7; i++)
                    parts.Add(art.Star("Templo · estrela " + (i + 1), Vector2.zero, new Vector2(.10f, .10f),
                        new Color(1f, .91f, .65f), 13, parent));
            }
            else if (activeTheme == TemaArena2D.RuinasAntigas)
            {
                parts.Add(art.Ring("Ruínas · poeira", new Vector2(0, -.12f), new Vector2(.42f, .11f), new Color(.83f, .72f, .52f), 12, parent));
                for (int i = 0; i < 7; i++)
                    parts.Add(art.Box("Ruínas · pedra " + (i + 1), Vector2.zero, new Vector2(.08f, .10f),
                        new Color(.79f, .70f, .55f), 13, parent));
            }
            else if (activeTheme == TemaArena2D.PantanoVenenoso)
            {
                parts.Add(art.Ring("Pântano · onda", new Vector2(0, -.12f), new Vector2(.42f, .11f), new Color(.58f, 1f, .25f), 12, parent));
                for (int i = 0; i < 7; i++)
                    parts.Add(art.Disk("Pântano · bolha " + (i + 1), Vector2.zero, .08f,
                        new Color(.58f, 1f, .29f), 13, parent));
            }
            else if (activeTheme == TemaArena2D.Deserto)
            {
                parts.Add(art.Ring("Deserto · poeira", new Vector2(0, -.12f), new Vector2(.42f, .11f), new Color(1f, .79f, .42f), 12, parent));
                for (int i = 0; i < 7; i++)
                    parts.Add(art.Disk("Deserto · areia " + (i + 1), Vector2.zero, .06f,
                        new Color(1f, .83f, .51f), 13, parent));
            }
            else if (activeTheme == TemaArena2D.Galaxia)
            {
                parts.Add(art.Ring("Galáxia · portal", new Vector2(0, -.12f), new Vector2(.42f, .11f), new Color(.74f, .56f, 1f), 12, parent));
                for (int i = 0; i < 7; i++)
                    parts.Add(art.Star("Galáxia · estrela " + (i + 1), Vector2.zero, new Vector2(.10f, .10f),
                        new Color(.85f, .76f, 1f), 13, parent));
            }
            else if (activeTheme == TemaArena2D.CidadeNeon)
            {
                parts.Add(art.Ring("Neon · brilho", new Vector2(0, -.12f), new Vector2(.42f, .11f), new Color(.96f, .36f, 1f), 12, parent));
                for (int i = 0; i < 7; i++)
                    parts.Add(art.Box("Neon · pixel " + (i + 1), Vector2.zero, new Vector2(.07f, .07f),
                        new Color(.38f, .94f, 1f), 13, parent));
            }
            else if (activeTheme == TemaArena2D.MundoDosDoces)
            {
                parts.Add(art.Ring("Doces · brilho", new Vector2(0, -.12f), new Vector2(.42f, .11f), new Color(1f, .57f, .80f), 12, parent));
                for (int i = 0; i < 7; i++)
                    parts.Add(art.Box("Doces · confeito " + (i + 1), Vector2.zero, new Vector2(.05f, .11f),
                        new Color(1f, .64f, .83f), 13, parent));
            }
            else if (activeTheme == TemaArena2D.Piratas)
            {
                parts.Add(art.Ring("Piratas · brilho", new Vector2(0, -.12f), new Vector2(.42f, .11f), new Color(1f, .82f, .36f), 12, parent));
                for (int i = 0; i < 7; i++)
                    parts.Add(art.Disk("Piratas · moeda " + (i + 1), Vector2.zero, .10f,
                        new Color(1f, .84f, .35f), 13, parent));
            }
            else if (activeTheme == TemaArena2D.Halloween)
            {
                parts.Add(art.Ring("Halloween · névoa", new Vector2(0, -.12f), new Vector2(.42f, .11f), new Color(.78f, .58f, 1f), 12, parent));
                for (int i = 0; i < 7; i++)
                    parts.Add(art.Ghost("Halloween · fantasma " + (i + 1), Vector2.zero, new Vector2(.10f, .14f),
                        new Color(.91f, .83f, 1f), 13, parent));
            }
            foreach (var part in parts) part.enabled = false;
            return parts.ToArray();
        }
        private static void SetVisible(Marble b, bool value)
        {
            foreach (var sprite in b.visual.GetComponentsInChildren<SpriteRenderer>(true)) sprite.enabled = value;
        }

        private void FixedUpdate()
        {
            if (world == null || Finished) return;
            float dt = Time.fixedDeltaTime; elapsed += dt; RemainingSeconds = Mathf.Max(0, duracaoSegundos - elapsed);
            if (RemainingSeconds <= 0) { Finish(); return; }
            ApplyPendingImpacts();
            float nextAngle = (float)FlipperMotion2D.Angle(elapsed, velocidadeDaAlavanca);
            flipperAngularVelocity = (nextAngle - flipperAngle) / dt;
            flipperAngle = nextAngle; flipper.MoveRotation(flipperAngle);
            obstacle.MovePosition(new Vector2((float)ArenaSurface2D.ObstacleX, (float)ArenaSurface2D.ObstacleY(elapsed, velocidadeDaBarreira)));
            UpdateNet(lowNet, dt); UpdateNet(highNet, dt);
            foreach (var b in balls) Advance(b, dt);
            MoveBasket(lowBasket, elapsed); MoveBasket(highBasket, elapsed);
            if (regraDaLava == LavaRule2D.EliminadaNaPartida && AllEliminated()) Finish();
        }
        private void MoveBasket(BasketRig basket, float time)
        {
            // Score against the completed physics step before scheduling the next one.
            basket.previousOffset = basket.Offset;
            Point2D target = moverCestas ? FlipperRules2D.BasketOffset(time, basket.points) : new Point2D(0, 0);
            Vector2 offset = new Vector2((float)target.x, (float)target.y);
            basket.body.MovePosition(world.TransformPoint(offset));
            basket.net.body.MovePosition(world.TransformPoint(basket.netOrigin + offset));
            basket.baseRoot.localPosition = new Vector3(offset.x, 0, 0);
            basket.supportRoot.localScale = new Vector3(1, (basket.supportHeight + offset.y) / basket.supportHeight, 1);
        }
        private bool AllEliminated()
        {
            foreach (var b in balls) if (b.phase != Phase.Eliminated) return false;
            return true;
        }
        private void Advance(Marble b, float dt)
        {
            b.age += dt;
            if (b.phase == Phase.Eliminated) return;
            if (b.phase == Phase.Returning)
            {
                TryRespawn(b);
                return;
            }
            if (b.phase == Phase.Burning) { AnimateBurn(b); return; }
            Vector2 current = b.body.position;
            if (b.ignoringExit && b.age >= .18f) IgnoreExit(b, false);
            // A ball may leave through the wider opening after another ball's bounce.
            if (b.phase == Phase.InChute && current.x > -4.05f) { b.phase = Phase.Flight; b.age = 0; }
            if (b.phase == Phase.Flight)
            {
                int points = FlipperRules2D.PointsCrossedMoving(P(b.previous), P(current), BallRadius,
                    P(lowBasket.previousOffset), P(lowBasket.Offset), P(highBasket.previousOffset), P(highBasket.Offset));
                if (points > 0) Score(b, points);
            }
            ApplyNetResistance(b, current);
            UpdateBasketLayer(b, current);
            if (ArenaSurface2D.TouchesFloor(current.y, BallRadius))
            {
                float x = SurfaceContactX(b.previous, current);
                if (ArenaSurface2D.IsLava(x) && GetVelocity(b.body).y <= 0) { StartBurn(b); return; }
                if (!ArenaSurface2D.IsLava(x) && b.phase != Phase.InChute && b.floorAge < 0) b.floorAge = 0;
            }
            b.previous = current;
            if (b.floorAge >= 0)
            {
                b.floorAge += dt;
                float remaining = (float)ArenaSurface2D.FloorLifetime - b.floorAge;
                if (remaining <= 0) { RecoverOrEliminate(b, false); return; }
                if (remaining < .5f) SetBallAlpha(b, remaining / .5f);
            }
            if (b.phase == Phase.InChute)
            {
                if ((current - b.progressAnchor).sqrMagnitude > .0064f) { b.progressAnchor = current; b.stagnantTime = 0; }
                else b.stagnantTime += dt;
                // A gentle physical nudge is only used if a ball settles beside the pivot.
                if (b.stagnantTime > 1.2f && current.y < -2.25f)
                {
                    b.body.AddForce(new Vector2(.9f, 1.4f) * b.body.mass, ForceMode2D.Impulse);
                    b.stagnantTime = 0; b.progressAnchor = current;
                }
            }
            bool stuckInChute = b.phase == Phase.InChute && b.stagnantTime > 8f;
            if (current.y < -4.15f || current.x > 7.75f || current.x < -7.8f || current.y > 4.35f ||
                stuckInChute || (b.floorAge < 0 && (b.phase == Phase.Flight || b.phase == Phase.Scored) && b.age > 8f))
            {
                RecoverOrEliminate(b, stuckInChute); return;
            }
            b.body.AddForce(Vector2.down * (Gravity * b.body.mass), ForceMode2D.Force);
        }
        private static float SurfaceContactX(Vector2 previous, Vector2 current)
        {
            float y = (float)ArenaSurface2D.FloorY + BallRadius + .035f;
            if (previous.y > y && current.y < y && previous.y - current.y > .0001f)
                return Mathf.Lerp(previous.x, current.x, (previous.y - y) / (previous.y - current.y));
            return current.x;
        }
        private static void SetBallAlpha(Marble b, float alpha)
        {
            Color color = b.team < 0 ? Red : Blue; color.a = Mathf.Clamp01(alpha); b.ballSprite.color = color;
            b.reflection.color = new Color(1, 1, 1, .9f * Mathf.Clamp01(alpha));
        }
        private static void ResetBallVisual(Marble b)
        {
            SetBallAlpha(b, 1);
            b.ballSprite.sortingOrder = 10; b.reflection.sortingOrder = 11;
            b.fireOuter.enabled = false; b.fireCore.enabled = false;
            foreach (var part in b.elementParts) part.enabled = false;
            b.elementRoot.localScale = Vector3.one; b.elementRoot.localRotation = Quaternion.identity;
        }
        private void StartBurn(Marble b)
        {
            b.scoredBeforeBurn = b.phase == Phase.Scored;
            if (b.ignoringExit) IgnoreExit(b, false);
            ClearActiveNet(b);
            b.collider.enabled = false;
            SetVelocity(b.body, Vector2.zero); b.body.angularVelocity = 0;
            b.body.bodyType = RigidbodyType2D.Kinematic; b.burnOrigin = b.body.position;
            b.phase = Phase.Burning; b.age = 0;
            b.fireOuter.enabled = activeTheme == TemaArena2D.Fogo;
            b.fireCore.enabled = activeTheme == TemaArena2D.Fogo;
            foreach (var part in b.elementParts) part.enabled = true;
        }
        private void AnimateBurn(Marble b)
        {
            float p = Mathf.Clamp01(b.age / (float)ArenaSurface2D.BurnDuration);
            if (p >= 1) { RecoverOrEliminate(b, false); return; }
            if (activeTheme != TemaArena2D.Fogo) { AnimateElementImpact(b, p); return; }
            b.body.position = b.burnOrigin + Vector2.up * (.12f * p);
            Color color = Color.Lerp(b.team < 0 ? Red : Blue, new Color(1, .16f, .02f), Mathf.Clamp01(p * 2));
            color.a = 1 - p; b.ballSprite.color = color;
            b.reflection.color = new Color(1, 1, 1, .9f * (1 - p));
            float flicker = 1 + .10f * Mathf.Sin(b.age * 24f);
            b.fireOuter.transform.localPosition = new Vector3(.025f * Mathf.Sin(b.age * 19f), .12f + .08f * p, 0);
            b.fireCore.transform.localPosition = new Vector3(0, .11f + .07f * p, 0);
            b.fireOuter.transform.localScale = new Vector3(.35f * flicker * (1 - .55f * p), .48f * (1 - .45f * p), 1);
            b.fireCore.transform.localScale = new Vector3(.20f * flicker * (1 - .60f * p), .29f * (1 - .50f * p), 1);
            b.fireOuter.color = new Color(1, .24f, .025f, .85f * (1 - p));
            b.fireCore.color = new Color(1, .84f, .18f, .95f * (1 - p));
        }
        private void AnimateElementImpact(Marble b, float p)
        {
            // The same recovery timer and motion are used in all themes; only the visuals differ.
            b.body.position = b.burnOrigin + Vector2.up * (.12f * p);
            Color tint;
            switch (activeTheme)
            {
                case TemaArena2D.Agua: tint = new Color(.40f, .87f, 1f); break;
                case TemaArena2D.Selva: tint = new Color(.60f, .91f, .42f); break;
                case TemaArena2D.Vento: tint = new Color(.83f, .91f, .97f); break;
                case TemaArena2D.Gelo: tint = new Color(.65f, .91f, 1f); break;
                case TemaArena2D.Caverna: tint = new Color(.70f, .65f, .85f); break;
                case TemaArena2D.TemploCelestial: tint = new Color(1f, .92f, .70f); break;
                case TemaArena2D.RuinasAntigas: tint = new Color(.82f, .76f, .60f); break;
                case TemaArena2D.PantanoVenenoso: tint = new Color(.65f, 1f, .36f); break;
                case TemaArena2D.Deserto: tint = new Color(1f, .84f, .55f); break;
                case TemaArena2D.Galaxia: tint = new Color(.79f, .68f, 1f); break;
                case TemaArena2D.CidadeNeon: tint = new Color(.66f, .80f, 1f); break;
                case TemaArena2D.MundoDosDoces: tint = new Color(1f, .70f, .85f); break;
                case TemaArena2D.Piratas: tint = new Color(1f, .88f, .52f); break;
                case TemaArena2D.Halloween: tint = new Color(.83f, .72f, 1f); break;
                default: tint = new Color(.72f, .80f, 1f); break;
            }
            Color ballColor = Color.Lerp(b.team < 0 ? Red : Blue, tint, p * .65f);
            ballColor.a = 1 - p; b.ballSprite.color = ballColor;
            b.reflection.color = new Color(1, 1, 1, .9f * (1 - p));
            if (activeTheme == TemaArena2D.Agua)
            {
                var ringPart = b.elementParts[0];
                ringPart.transform.localScale = new Vector3(.35f + .65f * p, .10f + .13f * p, 1);
                ringPart.color = new Color(.38f, .91f, 1f, .9f * (1 - p));
                for (int i = 1; i < b.elementParts.Length; i++)
                {
                    float angle = Mathf.Lerp(25f, 155f, (i - 1) / 6f) * Mathf.Deg2Rad;
                    float distance = .10f + .58f * p;
                    var drop = b.elementParts[i];
                    drop.transform.localPosition = new Vector3(Mathf.Cos(angle) * distance,
                        -.08f + Mathf.Sin(angle) * distance - .18f * p * p, 0);
                    drop.transform.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg - 90f);
                    float size = .075f * (1 - .45f * p);
                    drop.transform.localScale = new Vector3(size * .72f, size * 1.35f, 1);
                    drop.color = new Color(.55f, .94f, 1f, .95f * (1 - p));
                }
            }
            else if (activeTheme == TemaArena2D.Eletricidade)
            {
                float flash = .40f + .60f * Mathf.Abs(Mathf.Sin(b.age * 31f));
                float scale = 1 + .12f * Mathf.Sin(b.age * 24f);
                b.elementRoot.localScale = new Vector3(scale, scale, 1);
                b.elementRoot.localRotation = Quaternion.Euler(0, 0, 7f * Mathf.Sin(b.age * 20f));
                for (int i = 0; i < b.elementParts.Length; i++)
                    b.elementParts[i].color = i == 0 ? new Color(.30f, .54f, 1f, .38f * flash * (1 - p)) :
                        new Color(.70f, .91f, 1f, flash * (1 - p));
            }
            else if (activeTheme == TemaArena2D.Selva)
            {
                var ringPart = b.elementParts[0];
                ringPart.transform.localScale = new Vector3(.35f + .60f * p, .10f + .12f * p, 1);
                ringPart.color = new Color(.49f, .92f, .30f, .75f * (1 - p));
                for (int i = 1; i < b.elementParts.Length; i++)
                {
                    float angle = Mathf.Lerp(25f, 155f, (i - 1) / 6f) * Mathf.Deg2Rad;
                    float distance = .10f + .60f * p;
                    var leafPart = b.elementParts[i];
                    leafPart.transform.localPosition = new Vector3(Mathf.Cos(angle) * distance + .035f * Mathf.Sin(b.age * 13f + i),
                        -.08f + Mathf.Sin(angle) * distance - .22f * p * p, 0);
                    leafPart.transform.localRotation = Quaternion.Euler(0, 0, i * 52f + 220f * p);
                    float size = .115f * (1 - .35f * p);
                    leafPart.transform.localScale = new Vector3(size * .72f, size * 1.32f, 1);
                    leafPart.color = new Color(.34f + .035f * (i % 3), .87f, .22f, .95f * (1 - p));
                }
            }
            else if (activeTheme == TemaArena2D.Vento)
            {
                for (int i = 0; i < b.elementParts.Length; i++)
                {
                    var part = b.elementParts[i];
                    if (i < 4)
                    {
                        part.transform.localPosition = new Vector3(.035f * Mathf.Sin(b.age * 9f + i), -.12f + .08f * i, 0);
                        part.transform.localRotation = Quaternion.Euler(0, 0, -22f + i * 24f + 150f * p);
                        part.transform.localScale = new Vector3(.40f + .07f * i + .34f * p, .10f + .025f * i, 1);
                    }
                    else
                    {
                        part.transform.localPosition = new Vector3(.16f * Mathf.Sin(b.age * 14f + i), -.13f + .085f * (i - 4), 0);
                        part.transform.localRotation = Quaternion.Euler(0, 0, 9f * Mathf.Sin(b.age * 10f + i));
                        part.transform.localScale = new Vector3(.14f + .20f * (1 - p), .022f, 1);
                    }
                    part.color = new Color(.85f, .95f, 1f, .80f * (1 - p));
                }
            }
            else if (activeTheme == TemaArena2D.Gelo)
            {
                b.elementParts[0].color = new Color(.42f, .85f, 1f, .32f * (1 - p));
                for (int i = 1; i < b.elementParts.Length; i++)
                {
                    float angle = Mathf.Lerp(20f, 160f, (i - 1) / 6f) * Mathf.Deg2Rad;
                    float distance = .10f + .56f * p;
                    var shard = b.elementParts[i];
                    shard.transform.localPosition = new Vector3(Mathf.Cos(angle) * distance,
                        -.08f + Mathf.Sin(angle) * distance - .16f * p * p, 0);
                    shard.transform.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg - 90f + 100f * p);
                    float size = .105f * (1 - .40f * p);
                    shard.transform.localScale = new Vector3(size * .70f, size * 1.45f, 1);
                    shard.color = new Color(.72f, .95f, 1f, .95f * (1 - p));
                }
            }
            else if (activeTheme == TemaArena2D.Caverna || activeTheme == TemaArena2D.TemploCelestial ||
                activeTheme == TemaArena2D.RuinasAntigas || activeTheme == TemaArena2D.PantanoVenenoso ||
                activeTheme == TemaArena2D.Deserto || activeTheme == TemaArena2D.Galaxia ||
                activeTheme == TemaArena2D.CidadeNeon || activeTheme == TemaArena2D.MundoDosDoces ||
                activeTheme == TemaArena2D.Piratas || activeTheme == TemaArena2D.Halloween)
            {
                AnimateNewThemeImpact(b, p);
            }
        }
        private void AnimateNewThemeImpact(Marble b, float p)
        {
            Color color;
            switch (activeTheme)
            {
                case TemaArena2D.Caverna: color = new Color(.72f, .64f, .88f); break;
                case TemaArena2D.TemploCelestial: color = new Color(1f, .91f, .65f); break;
                case TemaArena2D.RuinasAntigas: color = new Color(.79f, .70f, .55f); break;
                case TemaArena2D.PantanoVenenoso: color = new Color(.58f, 1f, .29f); break;
                case TemaArena2D.Galaxia: color = new Color(.85f, .76f, 1f); break;
                case TemaArena2D.CidadeNeon: color = new Color(.56f, .90f, 1f); break;
                case TemaArena2D.MundoDosDoces: color = new Color(1f, .64f, .83f); break;
                case TemaArena2D.Piratas: color = new Color(1f, .84f, .35f); break;
                case TemaArena2D.Halloween: color = new Color(.91f, .83f, 1f); break;
                default: color = new Color(1f, .83f, .51f); break;
            }
            var ringPart = b.elementParts[0];
            ringPart.transform.localScale = new Vector3(.34f + .60f * p, .10f + .12f * p, 1);
            Color ringColor = color; ringColor.a = .80f * (1 - p); ringPart.color = ringColor;
            for (int i = 1; i < b.elementParts.Length; i++)
            {
                float angle = Mathf.Lerp(24f, 156f, (i - 1) / 6f) * Mathf.Deg2Rad;
                float distance = .10f + .58f * p;
                float x = Mathf.Cos(angle) * distance;
                float y = -.08f + Mathf.Sin(angle) * distance - .22f * p * p;
                float rotation = i * 42f + 150f * p;
                float width = .08f * (1 - .35f * p), height = .11f * (1 - .35f * p);
                if (activeTheme == TemaArena2D.TemploCelestial)
                {
                    x += .035f * Mathf.Sin(b.age * 9f + i);
                    y += .18f * p;
                    width = height = (.065f + .045f * Mathf.Abs(Mathf.Sin(b.age * 24f + i))) * (1 - .35f * p);
                    rotation = i * 35f + 80f * p;
                }
                else if (activeTheme == TemaArena2D.PantanoVenenoso)
                {
                    x = Mathf.Cos(angle) * (.06f + .28f * p) + .03f * Mathf.Sin(b.age * 11f + i);
                    y = -.08f + (.20f + .45f * Mathf.Sin(angle)) * p;
                    width = height = .055f + .055f * p;
                    rotation = 0;
                }
                else if (activeTheme == TemaArena2D.Deserto)
                {
                    x += .07f * Mathf.Sin(b.age * 9f + i) * p;
                    y -= .05f * p;
                    width = height = .045f + .03f * p;
                    rotation = 0;
                }
                else if (activeTheme == TemaArena2D.Galaxia)
                {
                    float orbit = angle + 2.2f * p;
                    x = Mathf.Cos(orbit) * (.10f + .52f * p);
                    y = -.06f + Mathf.Sin(orbit) * (.10f + .52f * p) + .14f * p;
                    width = height = (.065f + .04f * Mathf.Abs(Mathf.Sin(b.age * 24f + i))) * (1 - .35f * p);
                    rotation = i * 35f + 150f * p;
                }
                else if (activeTheme == TemaArena2D.CidadeNeon)
                {
                    x += .06f * Mathf.Sin(b.age * 33f + i);
                    y += .04f * Mathf.Sin(b.age * 26f + i * 2f);
                    width = height = .045f + .025f * Mathf.Abs(Mathf.Sin(b.age * 30f + i));
                    rotation = 0;
                }
                else if (activeTheme == TemaArena2D.MundoDosDoces)
                {
                    width = .045f * (1 - .25f * p); height = .105f * (1 - .25f * p);
                    rotation = i * 43f + 270f * p;
                }
                else if (activeTheme == TemaArena2D.Piratas)
                {
                    width = (.035f + .065f * Mathf.Abs(Mathf.Cos(b.age * 24f + i))) * (1 - .25f * p);
                    height = .10f * (1 - .25f * p); rotation = i * 17f;
                }
                else if (activeTheme == TemaArena2D.Halloween)
                {
                    x = Mathf.Cos(angle) * (.10f + .30f * p) + .04f * Mathf.Sin(b.age * 9f + i);
                    y = -.06f + .48f * p + .07f * Mathf.Sin(angle);
                    width = .09f * (1 - .20f * p); height = .13f * (1 - .20f * p);
                    rotation = 8f * Mathf.Sin(b.age * 7f + i);
                }
                var part = b.elementParts[i];
                part.transform.localPosition = new Vector3(x, y, 0);
                part.transform.localRotation = Quaternion.Euler(0, 0, rotation);
                part.transform.localScale = new Vector3(width, height, 1);
                Color particleColor = color;
                if (activeTheme == TemaArena2D.CidadeNeon && i % 2 == 0) particleColor = new Color(1f, .40f, .92f);
                if (activeTheme == TemaArena2D.MundoDosDoces)
                    particleColor = i % 3 == 0 ? new Color(1f, .85f, .30f) :
                        i % 3 == 1 ? new Color(1f, .55f, .80f) : new Color(.50f, .90f, 1f);
                particleColor.a = .95f * (1 - p); part.color = particleColor;
            }
        }
        private void Spawn(Marble b, Point2D point)
        {
            b.body.position = new Vector2((float)point.x, (float)point.y); b.body.bodyType = RigidbodyType2D.Dynamic;
            SetVelocity(b.body, Vector2.zero); b.body.angularVelocity = 0; b.collider.enabled = true;
            if (b.ignoringExit) IgnoreExit(b, false);
            SetNetIgnored(b, lowNet, true); SetNetIgnored(b, highNet, true); b.activeNet = 0;
            b.previous = b.body.position; b.progressAnchor = b.previous; b.stagnantTime = 0;
            b.floorAge = -1; b.scoredBeforeBurn = false; b.scoredHoop = 0;
            b.phase = Phase.InChute; b.age = 0; SetVisible(b, true); ResetBallVisual(b);
        }
        private void TryRespawn(Marble b)
        {
            const float minimum = BallRadius * 2f + .02f;
            for (int i = 0; i < 48; i++)
            {
                var candidate = SpawnField2D.Candidate(random, BallRadius, elapsed, velocidadeDoNascimento, i);
                bool free = true;
                foreach (var other in balls)
                {
                    if (other == b || other.phase == Phase.Returning || other.phase == Phase.Eliminated) continue;
                    Vector2 diff = other.body.position - new Vector2((float)candidate.x, (float)candidate.y);
                    if (diff.sqrMagnitude < minimum * minimum) { free = false; break; }
                }
                if (free) { Spawn(b, candidate); return; }
            }
        }
        internal void RecordFlipperContact(Rigidbody2D body)
        {
            // Collision callbacks occur during the physics step. Process each contact once on the next step.
            if (Finished || flipperAngularVelocity <= 5 || body == null) return;
            if (ballLookup.TryGetValue(body, out var b) && b.phase == Phase.InChute && !pendingImpacts.ContainsKey(b))
                pendingImpacts.Add(b, flipperAngle);
        }
        private void ApplyPendingImpacts()
        {
            foreach (var impact in pendingImpacts)
            {
                var b = impact.Key;
                if (b.phase != Phase.InChute || !b.collider.enabled) continue;
                Vector2 incoming = GetVelocity(b.body), position = b.body.position;
                var desired = FlipperMotion2D.AssistedVelocity(P(position), P(incoming), impact.Value, variacaoArremesso,
                    random.NextDouble() * 2 - 1, random.NextDouble() * 2 - 1);
                // The body remains dynamic. The impulse depends on the actual contact and the lever's angle.
                Vector2 change = new Vector2((float)desired.x, (float)desired.y) - incoming;
                IgnoreExit(b, true);
                b.body.AddForce(change * b.body.mass, ForceMode2D.Impulse);
                b.previous = position; b.phase = Phase.Flight; b.age = 0;
            }
            pendingImpacts.Clear();
        }
        private void IgnoreExit(Marble b, bool value)
        {
            if (flipperCollider != null) Physics2D.IgnoreCollision(b.collider, flipperCollider, value);
            b.ignoringExit = value;
        }
        private void Score(Marble b, int points)
        {
            if (b.team < 0) RedScore += points; else BlueScore += points;
            b.scoredHoop = points; b.phase = Phase.Scored; b.age = 0;
            var net = points == 3 ? highNet : lowNet;
            b.activeNet = points; SetNetIgnored(b, net, false);
            Vector2 incoming = GetVelocity(b.body);
            BasketRig basket = points == 3 ? highBasket : lowBasket;
            float center = (basket.left + basket.right) * .5f + basket.Offset.x;
            float offset = b.body.position.x - center;
            // The cloth absorbs part of the speed once the point has already been scored.
            float x = Mathf.Abs(offset) > .25f && offset * incoming.x > 0 ? -.15f * incoming.x : .25f * incoming.x;
            float y = Mathf.Min(-.3f, Mathf.Max(incoming.y * .65f, -4f));
            SetVelocity(b.body, new Vector2(x, y));
            float direction = Mathf.Abs(incoming.x) > .2f ? Mathf.Sign(incoming.x) : (offset >= 0 ? 1 : -1);
            net.angularVelocity = Mathf.Clamp(net.angularVelocity + direction * Mathf.Clamp(Mathf.Abs(incoming.x) * 7f + 9f, 20f, 50f), -60f, 60f);
        }
        private static void SetNetIgnored(Marble b, BasketNet net, bool ignored)
        {
            if (net == null || b.collider == null) return;
            Physics2D.IgnoreCollision(b.collider, net.left, ignored);
            Physics2D.IgnoreCollision(b.collider, net.right, ignored);
        }
        private void ClearActiveNet(Marble b)
        {
            if (b.activeNet == 0) return;
            SetNetIgnored(b, b.activeNet == 3 ? highNet : lowNet, true); b.activeNet = 0;
        }
        private void ApplyNetResistance(Marble b, Vector2 position)
        {
            if (b.activeNet == 0) return;
            BasketRig basket = b.activeNet == 3 ? highBasket : lowBasket;
            float y = basket.y + basket.Offset.y;
            if (position.y < y - .85f) { ClearActiveNet(b); return; }
            Vector2 velocity = GetVelocity(b.body);
            b.body.AddForce(new Vector2(-velocity.x * 2f * b.body.mass, 0), ForceMode2D.Force);
        }
        private static void UpdateNet(BasketNet net, float dt)
        {
            if (net == null || net.body == null) return;
            net.angularVelocity += -32f * net.angle * dt;
            net.angularVelocity *= Mathf.Exp(-5.5f * dt);
            net.angle = Mathf.Clamp(net.angle + net.angularVelocity * dt, -8f, 8f);
            net.body.MoveRotation(net.angle);
        }
        internal void RecordNetContact(int points, Rigidbody2D body)
        {
            if (body == null || !ballLookup.TryGetValue(body, out var ball) || ball.activeNet != points) return;
            var net = points == 3 ? highNet : lowNet;
            net.angularVelocity = Mathf.Clamp(net.angularVelocity + GetVelocity(body).x * 1.2f, -60f, 60f);
        }
        private void UpdateBasketLayer(Marble b, Vector2 position)
        {
            bool behindNet = false;
            if (b.phase == Phase.Scored && (b.scoredHoop == 2 || b.scoredHoop == 3))
            {
                BasketRig basket = b.scoredHoop == 3 ? highBasket : lowBasket;
                float left = basket.left + basket.Offset.x;
                float right = basket.right + basket.Offset.x;
                float y = basket.y + basket.Offset.y;
                // Keep the ball behind the transparent net only while it overlaps that basket.
                behindNet = position.x >= left - BallRadius && position.x <= right + BallRadius &&
                    position.y >= y - .62f - BallRadius && position.y <= y + BallRadius;
            }
            else if (b.phase == Phase.Flight && GetVelocity(b.body).y < 0)
            {
                // As the ball touches the mouth, place the front of the rim over it.
                behindNet = ApproachingRim(position, lowBasket.left + lowBasket.Offset.x,
                    lowBasket.right + lowBasket.Offset.x, lowBasket.y + lowBasket.Offset.y) ||
                    ApproachingRim(position, highBasket.left + highBasket.Offset.x,
                    highBasket.right + highBasket.Offset.x, highBasket.y + highBasket.Offset.y);
            }
            b.ballSprite.sortingOrder = behindNet ? 3 : 10;
            b.reflection.sortingOrder = behindNet ? 4 : 11;
        }
        private static bool ApproachingRim(Vector2 position, float left, float right, float y)
        {
            return position.x >= left + BallRadius && position.x <= right - BallRadius &&
                position.y >= y && position.y <= y + BallRadius;
        }
        private void RecoverOrEliminate(Marble b, bool stuckInChute)
        {
            bool madeBasket = b.phase == Phase.Scored || (b.phase == Phase.Burning && b.scoredBeforeBurn);
            if (b.ignoringExit) IgnoreExit(b, false);
            ClearActiveNet(b);
            b.collider.enabled = false; SetVelocity(b.body, Vector2.zero); b.body.angularVelocity = 0;
            b.body.bodyType = RigidbodyType2D.Kinematic; SetVisible(b, false);
            if (FlipperRules2D.Eliminate(madeBasket, stuckInChute, regraDaLava == LavaRule2D.EliminadaNaPartida)) b.phase = Phase.Eliminated;
            else b.phase = Phase.Returning;
            b.age = 0;
        }
        private void Finish()
        {
            Finished = true;
            foreach (var b in balls) { SetVelocity(b.body, Vector2.zero); b.body.angularVelocity = 0; b.body.simulated = false; }
        }
        private static Point2D P(Vector2 v) => new Point2D(v.x, v.y);
        private static Vector2 GetVelocity(Rigidbody2D body)
        {
#if UNITY_6000_0_OR_NEWER
            return body.linearVelocity;
#else
            return body.velocity;
#endif
        }
        private static void SetVelocity(Rigidbody2D body, Vector2 velocity)
        {
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = velocity;
#else
            body.velocity = velocity;
#endif
        }
        private void OnDestroy()
        {
            RestorePipeline(); if (art != null) art.Dispose();
            foreach (var asset in assets)
            {
                if (asset == null) continue;
                if (Application.isPlaying) Destroy(asset); else DestroyImmediate(asset);
            }
            assets.Clear();
        }
        private void RestorePipeline()
        {
            if (!pipelineAcquired) return;
            if (QualitySettings.renderPipeline == null) QualitySettings.renderPipeline = oldQuality;
            if (GraphicsSettings.defaultRenderPipeline == null) GraphicsSettings.defaultRenderPipeline = oldGraphics;
            pipelineAcquired = false;
        }
    }
}
