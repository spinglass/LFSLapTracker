using InSimDotNet;
using InSimDotNet.Helpers;
using InSimDotNet.Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LFSLapTracker
{
    class InSimConnection
    {
        public InSimConnection(Settings settings)
        {
            m_Settings = settings;
        }

        public void Run()
        {
            m_RaceTimeOffset = new Stopwatch();

            while (!m_Quit)
            {
                m_InSim = new InSim();
                m_InSim.Initialized += OnInitialized;
                m_InSim.Disconnected += OnDisconnected;
                m_InSim.InSimError += OnError;

                Bind<IS_NCN>(OnNewConnection);
                Bind<IS_NPL>(OnNewPlayer);
                Bind<IS_STA>(OnStateInfo);
                Bind<IS_PLP>(OnPlayerPits);
                Bind<IS_PLA>(OnPitLane);
                Bind<IS_RST>(OnRaceStart);
                Bind<IS_LAP>(OnLap);
                Bind<IS_SPX>(OnSplit);
                Bind<IS_MCI>(OnMultiCarInfo);
                Bind<IS_SMALL>(OnSmallPacket);

                Console.WriteLine("Attempting to connect...");

                while (!m_InSim.IsConnected && !m_Quit)
                {
                    try
                    {
                        m_InSim.Initialize(new InSimSettings
                        {
                            Host = m_Settings.Host,
                            Port = m_Settings.Port,
                            Admin = string.Empty,
                            Flags = InSimFlags.ISF_MCI | InSimFlags.ISF_LOCAL,
                            Interval = 10,
                        });

                        SendTiny(TinyType.TINY_SST);
                        SendTiny(TinyType.TINY_RST);
                    }
                    catch (Exception)
                    {
                    }

                    Thread.Sleep(1000);
                }

                while (m_InSim.IsConnected && !m_Quit)
                {
                    if (m_InGame && !m_InPits)
                    {
                        double raceTime = EstimatedRaceTime;
                        if (raceTime > m_NextUpdateTime)
                        {
                            LapTimeDelta delta = m_LapDelta;
                            if (delta.IsValid)
                            {
                                string colour;
                                if (delta.IsNegative)
                                {
                                    colour = "^2";
                                }
                                else
                                {
                                    colour = "^1";
                                }
                                SendButton(colour + delta);
                            }
                            else
                            {
                                ClearButtons();
                            }

                            m_NextUpdateTime = raceTime + 1.0;
                        }

                        // Request game time
                        SendTiny(TinyType.TINY_GTH);
                    }

                    Thread.Sleep(40);

                    if (m_InSim.IsConnected && m_Quit)
                    {
                        m_InSim.Disconnect();
                    }
                }
            }
        }

        public void Quit()
        {
            m_Quit = true;
        }

        private double EstimatedRaceTime
        {
            get { return m_RaceTime + m_RaceTimeOffset.Elapsed.TotalSeconds; }
        }

        private void Bind<T>(Action<InSim, T> callback) where T : IPacket
        {
            // Keep a reference to callbacks, as InSim will only keep a WeakReference, which may be garbage collected
            if (!m_Callbacks.Contains(callback))
            {
                m_Callbacks.Add(callback);
            }
            m_InSim.Bind<T>(callback);
        }

        private void SendTiny(TinyType type)
        {
            m_InSim.Send(new IS_TINY { SubT = type, ReqI = 1 });
        }

        private void SendMessage(string format, params object[] args)
        {
            string msg = string.Format(format, args);
            m_InSim.Send(new IS_MSL { Msg = "^3LAP TRACKER^7 " + msg });
            Console.WriteLine("LAP TRACKER " + msg);
        }

        private void SendButton(string format, params object[] args)
        {
            m_InSim.Send(new IS_BTN
            {
                ReqI = 1,
                ClickID = 1,
                BStyle = ButtonStyles.ISB_LIGHT,
                L = 75,
                T = 0,
                W = 50,
                H = 10,
                Text = string.Format(format, args)
            });
        }

        private void OnInitialized(object sender, InitializeEventArgs e)
        {
            SendMessage("Connected");
        }

        private void OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            Console.WriteLine("Connection lost ({0})", e.Reason);
        }

        private void OnError(object sender, InSimErrorEventArgs e)
        {
            Console.WriteLine("ERROR: {0}", e.Exception.Message);
        }

        private void OnNewConnection(InSim inSim, IS_NCN packet)
        {
            if (!packet.Remote)
            {
                Console.WriteLine("Found local connection : {0} - {1}", packet.UCID, packet.PName);
                SendMessage("Tracking " + packet.PName);
                m_LocalConnectionId = packet.UCID;
            }
        }

        private void OnNewPlayer(InSim inSim, IS_NPL packet)
        {
            if (packet.UCID == m_LocalConnectionId && (m_Settings.UseAI || packet.PType == 0 || packet.PType == PlayerTypes.PLT_FEMALE))
            {
                m_LocalPlayerId = packet.PLID;
                m_InPits = ((packet.Flags & PlayerFlags.PIF_INPITS) == PlayerFlags.PIF_INPITS);
                Console.WriteLine("Found local player : {0} - {1}", packet.PLID, m_InPits ? "pit" : "track");

                ResetPlayer();
            }
        }

        private void OnStateInfo(InSim inSim, IS_STA packet)
        {
            bool inGame = ((packet.Flags & StateFlags.ISS_GAME) == StateFlags.ISS_GAME);
            if (inGame && !m_InGame)
            {
                Console.WriteLine("Joined Game");
                SendTiny(TinyType.TINY_NCN);
                SendTiny(TinyType.TINY_NPL);
            }
            else if (!inGame && m_InGame)
            {
                Console.WriteLine("Left Game");
                ClearButtons();
                m_LocalPlayerId = byte.MaxValue;
            }

            if (packet.Track != null)
            {
                // Request full track info
                SendTiny(TinyType.TINY_RST);
            }

            m_InGame = inGame;
        }

        private void OnPlayerPits(InSim inSim, IS_PLP packet)
        {
            if (packet.PLID == m_LocalPlayerId)
            {
                Console.WriteLine("Local player pits");
                m_InPits = true;
                ClearButtons();
            }
        }

        private void OnPitLane(InSim inSim, IS_PLA packet)
        {
            if (packet.PLID == m_LocalPlayerId)
            {
                Console.WriteLine("Pit: {0}", packet.Fact);
            }
        }

        private void OnRaceStart(InSim inSim, IS_RST packet)
        {
            if (packet.ReqI == 0)
            {
                Console.WriteLine("Race start");
                ResetPlayer();

                if (m_BestSplitTimes != null)
                {
                    SendMessage("Best lap: " + m_BestSplitTimes.Last());
                }
            }
            else
            {
                if (m_Track == null || m_Track.ShortName != packet.Track)
                {
                    m_Track = new Track(packet);
                    Console.WriteLine("New track: {0} - {1}", m_Track.ShortName, m_Track.LongName);

                    m_BestSplitTimes = null;
                    m_BestNodeTimes = null;
                }
            }
        }

        private void OnLap(InSim inSim, IS_LAP packet)
        {
            // Bank the estimated race time as early as we can
            double raceTime = EstimatedRaceTime;

            if (packet.PLID == m_LocalPlayerId)
            {
                LapTime lapTime = new LapTime(packet.LTime.TotalSeconds);
                if (lapTime.IsValid)
                {
                    Console.WriteLine("Lap {0}: {1}", packet.LapsDone, lapTime);
                    OnSplit(lapTime, m_Track.NumSectors - 1, true);

                    if (m_BestSplitTimes == null || lapTime < m_BestSplitTimes.Last())
                    {
                        // Beat best lap (or first lap) - promote current split and node times to best
                        m_BestSplitTimes = m_CurrentSplitTimes;
                        m_BestNodeTimes = m_CurrentNodeTimes;

                        SendMessage("New best lap: " + lapTime);
                    }
                }
                else
                {
                    Console.WriteLine("New lap");
                }

                // Reset for new lap
                m_CurrentSplitTimes = new LapTime[m_Track.NumSectors];
                m_CurrentNodeTimes = new double[m_Track.NumNodes];
                m_NodeIndex = 0;
                m_LapStartTime = raceTime;
            }
        }

        private void OnSplit(InSim inSim, IS_SPX packet)
        {
            if (packet.PLID == m_LocalPlayerId)
            {
                LapTime splitTime = new LapTime(packet.STime.TotalSeconds);
                if (splitTime.IsValid)
                {
                    Console.WriteLine("Split {0}: {1}", packet.Split, splitTime);
                    OnSplit(splitTime, packet.Split - 1, false);
                }
                else
                {
                    Console.WriteLine("New split");
                }
            }
        }

        private void OnSplit(LapTime splitTime, int split, bool lap)
        {
            if (splitTime.IsValid)
            {
                m_CurrentSplitTimes[split] = splitTime;

                // Compare to best lap
                if (m_BestSplitTimes == null || !m_BestSplitTimes[split].IsValid)
                {
                    SendButton("^7" + splitTime);
                }
                else
                {
                    string colour;
                    LapTimeDelta delta = splitTime - m_BestSplitTimes[split];
                    if (delta.IsNegative)
                    {
                        // Beat best split
                        colour = "^2";
                    }
                    else
                    {
                        // Failed to beat best split
                        colour = "^1";
                    }
                    SendButton("^7{0}  {1}{2}", splitTime, colour, delta);
                }
            }

            // Show button for 6 seconds
            m_NextUpdateTime = m_RaceTime + 6.0;
        }

        private void OnMultiCarInfo(InSim inSim, IS_MCI packet)
        {
            // Bank the estimated race time as early as we can
            double raceTime = EstimatedRaceTime;

            if (m_InGame && !m_InPits && m_LocalPlayerId != byte.MaxValue)
            {
                foreach (CompCar car in packet.Info)
                {
                    if (car.PLID == m_LocalPlayerId)
                    {
                        // Convert to node index
                        int nodeIndex = m_Track.GetNodeIndex(car.Node);

                        if (m_NodeIndex == -1 && nodeIndex < s_MaxNodeJump)
                        {
                            Console.WriteLine("Starting first lap");
                            m_LapStartTime = raceTime;
                        }

                        if (m_NodeIndex < nodeIndex && nodeIndex < m_NodeIndex + s_MaxNodeJump)
                        {
                            // Estimate distance beyond node
                            TrackPath.Node node = m_Track.GetNode(nodeIndex);
                            TrackPath.Point carPos = new TrackPath.Point((double)car.X / 65536, (double)car.Y / 65536, (double)car.Z / 65536);
                            TrackPath.Vector nodeToCar = carPos - node.Centre;
                            double distance = nodeToCar.X * node.Direction.X + nodeToCar.Y * node.Direction.Y + nodeToCar.Z * node.Direction.Z;

                            // Estimate time from node
                            double speed = (double)car.Speed * 100.0 / 32768;
                            double timeFromNode = distance / speed;

                            m_CurrentNodeTimes[nodeIndex] = raceTime - m_LapStartTime - timeFromNode;
                            m_NodeIndex = nodeIndex;

                            if (m_BestNodeTimes != null)
                            {
                                if (m_CurrentNodeTimes[nodeIndex] > 0.0 && m_BestNodeTimes[nodeIndex] > 0.0)
                                {
                                    m_LapDelta = new LapTimeDelta(m_CurrentNodeTimes[nodeIndex] - m_BestNodeTimes[nodeIndex]);
                                }
                                else
                                {
                                    m_LapDelta = LapTimeDelta.Invalid;
                                }
                            }
                        }

                        break;
                    }
                }
            }
        }

        private void OnSmallPacket(InSim inSim, IS_SMALL packet)
        {
            if (packet.SubT == SmallType.SMALL_RTP)
            {
                m_RaceTimeOffset.Restart();
                m_RaceTime = 0.01 * packet.UVal;
            }
        }

        private void ResetPlayer()
        {
            m_NodeIndex = -1;
            m_LapDelta = LapTimeDelta.Invalid;
            if (m_Track != null)
            {
                m_CurrentSplitTimes = new LapTime[m_Track.NumSectors];
                m_CurrentNodeTimes = new double[m_Track.NumNodes];
            }
            ClearButtons();
        }

        private void ClearButtons()
        {
            m_InSim.Send(new IS_BFN
            {
                SubT = ButtonFunction.BFN_CLEAR
            });
        }

        private static int s_MaxNodeJump = 20;

        private Settings m_Settings;
        private InSim m_InSim;
        private List<object> m_Callbacks = new List<object>();
        private bool m_Quit;

        private byte m_LocalConnectionId;
        private byte m_LocalPlayerId;
        private bool m_InGame;
        private bool m_InPits;
        private Track m_Track;

        private int m_NodeIndex;

        private double m_RaceTime;
        private Stopwatch m_RaceTimeOffset;
        private double m_LapStartTime;
        private LapTimeDelta m_LapDelta;

        private double m_NextUpdateTime;

        private double[] m_BestNodeTimes;
        private double[] m_CurrentNodeTimes;

        private LapTime[] m_BestSplitTimes;
        private LapTime[] m_CurrentSplitTimes;
    }
}
