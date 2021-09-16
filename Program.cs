using System;
using System.Threading.Tasks;
using Microsoft.MixedReality.WebRTC;
using System.Collections.Generic;

namespace TestNetCoreConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            AudioTrackSource microphoneSource = null;
            VideoTrackSource webcamSource = null;
            Transceiver audioTransceiver = null;
            Transceiver videoTransceiver = null;
            LocalAudioTrack localAudioTrack = null;
            LocalVideoTrack localVideoTrack = null;
            try
            {
                bool needVideo = Array.Exists(args, arg => (arg == "-v") || (arg == "--video"));
                bool needAudio = Array.Exists(args, arg => (arg == "-a") || (arg == "--audio"));

                var deviceList = await DeviceVideoTrackSource.GetCaptureDevicesAsync();
                foreach(var device in deviceList)
                {
                    Console.WriteLine($"Found webcam {device.name} (id: {device.id})");
                }           

                using var pc = new PeerConnection();
                var signaler = new NamedPipeSignaler.NamedPipeSignaler(pc, "testpipe");

                var config = new PeerConnectionConfiguration
                {
                    IceServers = new List<IceServer>
                    {
                        new IceServer{Urls = {"stun:stun.l.google.com:19302"} }
                    }
                };
                await pc.InitializeAsync(config);
                Console.WriteLine("Peer connection done");

                if (needVideo)
                {
                    Console.WriteLine("Opening Webcam...");
                    webcamSource = await DeviceVideoTrackSource.CreateAsync();
                    var videoTrackConfig = new LocalVideoTrackInitConfig
                    {
                        trackName = "webcam_track"
                    };            
                    localVideoTrack = LocalVideoTrack.CreateFromSource(webcamSource, videoTrackConfig);
                    videoTransceiver = pc.AddTransceiver(MediaKind.Video);
                    videoTransceiver.LocalVideoTrack = localVideoTrack;
                    videoTransceiver.DesiredDirection = Transceiver.Direction.SendReceive;
                }
                if(needAudio)
                {
                    Console.WriteLine("Opening local microphone...");
                    microphoneSource = await DeviceAudioTrackSource.CreateAsync();
                    var audioTrackConfig = new LocalAudioTrackInitConfig
                    {
                        trackName = "microphone_track"
                    };
                    localAudioTrack = LocalAudioTrack.CreateFromSource(microphoneSource, audioTrackConfig);
                    audioTransceiver = pc.AddTransceiver(MediaKind.Audio);
                    audioTransceiver.LocalAudioTrack = localAudioTrack;
                    audioTransceiver.DesiredDirection = Transceiver.Direction.SendReceive;
                }
                

                

                signaler.SdpMessageReceived += async (SdpMessage message) =>
                {
                    await pc.SetRemoteDescriptionAsync(message);
                    if (message.Type == SdpMessageType.Offer)
                    {
                        pc.CreateAnswer();
                    }
                };

                signaler.IceCandidateReceived += (IceCandidate candidate) =>
                {
                    pc.AddIceCandidate(candidate);
                };

                await signaler.StartAsync();

                localAudioTrack?.Dispose();
                localVideoTrack?.Dispose();
                microphoneSource?.Dispose();
                webcamSource?.Dispose();
                

            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
