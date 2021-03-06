﻿using MaterialDesignThemes.Wpf;
using Pace.Common.Network.Packets;
using Pace.Common.Network.Packets.Client;
using Pace.Common.Network.Packets.Server;
using Pace.Server.Model;
using Pace.Server.Network;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace Pace.Server.ViewModel
{
    public class ClientViewModel : ViewModelBase
    {
        private readonly PaceServer server;
        private readonly FileWindowService fileManagerService;

        private ClientInfo selectedClient;
        public ClientInfo SelectedClient
        {
            get { return selectedClient; }
            set
            {
                selectedClient = value;
                OnPropertyChanged(() => selectedClient);
            }
        }

        public ObservableCollection<ClientInfo> Clients { get; set; }

        public SnackbarMessageQueue ConnectedMessageQueue { get; set; }

        public RelayCommand<ClientInfo> OpenFileManagerCommand { get; set; }

        public ClientViewModel()
        {
            fileManagerService = new FileWindowService();

            Clients = new ObservableCollection<ClientInfo>();

            ConnectedMessageQueue = new SnackbarMessageQueue(TimeSpan.FromMilliseconds(5000));

            OpenFileManagerCommand = new RelayCommand<ClientInfo>(OpenFileManager);

            #if DEBUG
                if (DesignerProperties.GetIsInDesignMode(new DependencyObject())) return;
            #endif

            server = new PaceServer();
            server.ClientConnected += Server_ClientConnected;
            server.ClientDisconnected += Server_ClientDisconnected;

            server.PacketChannel.RegisterHandler<GetSystemInfoResponsePacket>(HandleSystemInfo);

            server.Start();
        }

        private void Server_ClientConnected(object sender, ClientEventArgs e)
        {
            ConnectedMessageQueue.Enqueue($"Client connected from {e.Client.TcpClient.Client.RemoteEndPoint}.");

            e.Client.SendPacket(new GetSystemInfoRequestPacket());
        }

        private void Server_ClientDisconnected(object sender, ClientEventArgs e)
        {
            var list = new List<ClientInfo>(Clients);
            var client = list.Find(c => c.Owner == e.Client);

            Application.Current.Dispatcher.Invoke(() =>
            {
                Clients.Remove(client);
            });
        }

        private void OpenFileManager(ClientInfo client)
        {
            fileManagerService.ShowWindow(server, SelectedClient);
        }

        private void HandleSystemInfo(IPacket packet)
        {
            var systemInfoResponse = (GetSystemInfoResponsePacket)packet;

            var clientInfo = new ClientInfo()
            {
                Identifier = systemInfoResponse.Identifier,
                Address = systemInfoResponse.Address,
                Port = systemInfoResponse.Port,
                Username = systemInfoResponse.Username,
                ComputerName = systemInfoResponse.ComputerName,
                OS = systemInfoResponse.OS
            };

            clientInfo.Owner = server.ConnectedClients.Find(c => clientInfo.Address == c.RemoteAddress.Split(':')[0]);

            Application.Current.Dispatcher.Invoke(() =>
            {
                Clients.Add(clientInfo);
            });
        }
    }
}