# BlazeJump Tools

A comprehensive toolkit for Nostr protocol integration including relay management, cryptographic services, message handling, and user profile management for Blazor applications.

## Features

- **Relay Management** - Connect to and manage multiple Nostr relays simultaneously
- **Cryptographic Services** - Built-in support for Nostr key management and signing
- **Message Handling** - Send and receive Nostr events with ease
- **User Profile Management** - Handle Nostr user profiles and metadata
- **Event Filtering** - Advanced filtering capabilities for querying events
- **Dependency Injection** - Easy service registration for .NET applications

## Installation

### Step 1: Install the NuGet Package

Install the `BlazeJump.Tools` package using one of the following methods:

**Via .NET CLI:**
```bash
dotnet add package BlazeJump.Tools
```

**Via Package Manager Console:**
```powershell
Install-Package BlazeJump.Tools
```

**Via PackageReference in .csproj:**
```xml
<PackageReference Include="BlazeJump.Tools" Version="1.0.0" />
```

### Step 2: Configure Services

In your `Program.cs` or startup configuration, register the BlazeJump services:

```csharp
using BlazeJump.Tools;

var builder = WebApplication.CreateBuilder(args);

// Add BlazeJump services
builder.Services.ConfigureServices();

var app = builder.Build();
```

## Getting Started

### Basic Usage: Connecting to a Relay

Here's a simple example of connecting to a Nostr relay and processing messages:

```csharp
using BlazeJump.Tools.Services.Connections;
using BlazeJump.Tools.Models;
using Microsoft.Extensions.DependencyInjection;

public class NostrExample
{
    private readonly IRelayManager _relayManager;
    
    public NostrExample(IRelayManager relayManager)
    {
        _relayManager = relayManager;
        
        // Subscribe to the message processing event
        _relayManager.ProcessMessageQueue += OnProcessMessageQueue;
    }
    
    public async Task ConnectToRelayAsync()
    {
        // Step 1: Add a relay URI
        string relayUri = "wss://relay.damus.io";
        bool added = _relayManager.TryAddUri(relayUri);
        
        if (added)
        {
            Console.WriteLine($"Successfully added relay: {relayUri}");
        }
        
        // Step 2: Open the connection
        await _relayManager.OpenConnection(relayUri);
        Console.WriteLine($"Connected to {relayUri}");
    }
    
    private void OnProcessMessageQueue(object? sender, EventArgs e)
    {
        // Process messages from the queue
        while (_relayManager.ReceivedMessages.TryDequeue(out NMessage? message))
        {
            if (message?.Event != null)
            {
                Console.WriteLine($"Received event: {message.Event.Id}");
                Console.WriteLine($"Author: {message.Event.Pubkey}");
                Console.WriteLine($"Content: {message.Event.Content}");
                Console.WriteLine($"Kind: {message.Event.Kind}");
            }
        }
    }
}
```

### Querying Events with Filters

Query events from relays using filters:

```csharp
using BlazeJump.Tools.Models;
using BlazeJump.Tools.Enums;

public async Task QueryEventsAsync()
{
    // Create a filter to get recent events
    var filter = new Filter
    {
        Kinds = new List<int> { (int)KindEnum.TextNote }, // Kind 1 = Text Notes
        Limit = 50,
        Since = DateTime.UtcNow.AddHours(-1) // Events from the last hour
    };
    
    // Query relays with the filter
    string subscriptionId = Guid.NewGuid().ToString();
    await _relayManager.QueryRelays(
        subscriptionId: subscriptionId,
        requestMessageType: MessageTypeEnum.REQ,
        filters: new List<Filter> { filter },
        timeout: 15000 // 15 seconds timeout
    );
    
    Console.WriteLine("Query sent to relays. Messages will arrive via ProcessMessageQueue event.");
}
```

### Sending Events to Relays

Send a Nostr event to all connected relays:

```csharp
using BlazeJump.Tools.Models;
using BlazeJump.Tools.Enums;

public async Task SendEventAsync(NEvent nEvent, string subscriptionHash)
{
    // The event should be properly signed before sending
    await _relayManager.SendNEvent(nEvent, subscriptionHash);
    
    Console.WriteLine($"Event {nEvent.Id} sent to all connected relays");
}
```

### Advanced Filter Examples

**Filter by Author:**
```csharp
var filter = new Filter
{
    Authors = new List<string> 
    { 
        "pubkey1_hex_encoded",
        "pubkey2_hex_encoded"
    },
    Limit = 100
};
```

**Filter by Event IDs:**
```csharp
var filter = new Filter
{
    EventIds = new List<string> 
    { 
        "event_id_1",
        "event_id_2"
    }
};
```

**Filter by Tagged Users (Mentions):**
```csharp
var filter = new Filter
{
    TaggedPublicKeys = new List<string> { "user_pubkey_hex" },
    Kinds = new List<int> { (int)KindEnum.TextNote }
};
```

**Filter by Hashtags:**
```csharp
var filter = new Filter
{
    TaggedKeywords = new List<string> { "nostr", "bitcoin" },
    Limit = 20
};
```

**Time-based Filtering:**
```csharp
var filter = new Filter
{
    Since = DateTime.UtcNow.AddDays(-7), // Events from last week
    Until = DateTime.UtcNow,
    Kinds = new List<int> { (int)KindEnum.TextNote },
    Limit = 100
};
```

## Complete Example

Here's a complete example that puts it all together:

```csharp
using BlazeJump.Tools;
using BlazeJump.Tools.Services.Connections;
using BlazeJump.Tools.Models;
using BlazeJump.Tools.Enums;
using Microsoft.Extensions.DependencyInjection;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.ConfigureServices();
        var serviceProvider = services.BuildServiceProvider();
        
        // Get the relay manager
        var relayManager = serviceProvider.GetRequiredService<IRelayManager>();
        
        // Subscribe to message queue processing
        relayManager.ProcessMessageQueue += (sender, e) =>
        {
            while (relayManager.ReceivedMessages.TryDequeue(out NMessage? message))
            {
                if (message?.Event != null)
                {
                    Console.WriteLine($"New Event: {message.Event.Content}");
                }
            }
        };
        
        // Add and connect to relays
        var relays = new[]
        {
            "wss://relay.damus.io",
            "wss://nos.lol",
            "wss://relay.nostr.band"
        };
        
        foreach (var relay in relays)
        {
            if (relayManager.TryAddUri(relay))
            {
                await relayManager.OpenConnection(relay);
                Console.WriteLine($"Connected to {relay}");
            }
        }
        
        // Query for recent text notes
        var filter = new Filter
        {
            Kinds = new List<int> { (int)KindEnum.TextNote },
            Limit = 10,
            Since = DateTime.UtcNow.AddMinutes(-5)
        };
        
        await relayManager.QueryRelays(
            subscriptionId: "my-subscription",
            requestMessageType: MessageTypeEnum.REQ,
            filters: new List<Filter> { filter }
        );
        
        Console.WriteLine("Listening for events... Press Ctrl+C to exit");
        
        // Keep the application running
        await Task.Delay(Timeout.Infinite);
    }
}
```

## Cryptography and Key Management

### Creating a Key Pair

To work with Nostr events, you need a cryptographic key pair. The library provides the `ICryptoService` for key generation and cryptographic operations:

```csharp
using BlazeJump.Tools.Services.Crypto;
using Microsoft.Extensions.DependencyInjection;

// Get the crypto service from DI
var cryptoService = serviceProvider.GetRequiredService<ICryptoService>();

// Create an ethereal (temporary) key pair
cryptoService.CreateEtherealKeyPair();

// Get the public key in hex format
var publicKeyBytes = cryptoService.EtherealPublicKey?.ToXOnlyPubKey().ToBytes();
var publicKeyHex = Convert.ToHexString(publicKeyBytes).ToLower();

Console.WriteLine($"Public Key: {publicKeyHex}");
```

### Creating and Signing Events

Use the `IMessageService` to create and sign Nostr events. The service will automatically handle signing with your key pair:

```csharp
using BlazeJump.Tools.Services.Message;
using BlazeJump.Tools.Services.Crypto;
using BlazeJump.Tools.Enums;

// Get required services
var messageService = serviceProvider.GetRequiredService<IMessageService>();
var cryptoService = serviceProvider.GetRequiredService<ICryptoService>();

// Ensure you have a key pair
cryptoService.CreateEtherealKeyPair();

// Create a text note event
var textEvent = messageService.CreateNEvent(
    kind: KindEnum.TextNote,
    message: "Hello Nostr!",
    parentId: null,
    rootId: null,
    ptags: null
);

// Send the event (it will be signed automatically)
await messageService.Send(KindEnum.TextNote, textEvent);

Console.WriteLine($"Sent event with ID: {textEvent.Id}");
```

### Encrypting Event Content

For encrypted direct messages (Kind 4), you can encrypt the content before sending:

```csharp
using BlazeJump.Tools.Services.Crypto;
using BlazeJump.Tools.Services.Message;
using BlazeJump.Tools.Enums;

// Get services
var cryptoService = serviceProvider.GetRequiredService<ICryptoService>();
var messageService = serviceProvider.GetRequiredService<IMessageService>();

// Ensure you have a key pair
cryptoService.CreateEtherealKeyPair();

// Create an encrypted direct message
var recipientPubKey = "recipient_public_key_hex"; // The recipient's public key

var encryptedMessage = messageService.CreateNEvent(
    kind: KindEnum.EncryptedDirectMessage,
    message: "This is a private message",
    parentId: null,
    rootId: null,
    ptags: new List<string> { recipientPubKey } // Tag the recipient
);

// Send with encryption - the message content will be encrypted automatically
await messageService.Send(
    kind: KindEnum.EncryptedDirectMessage,
    nEvent: encryptedMessage,
    encryptPubKey: recipientPubKey
);

Console.WriteLine("Encrypted message sent!");
```

### Manual Encryption and Decryption

For more control over encryption, you can use the crypto service directly:

**Encrypting Text:**
```csharp
// Encrypt a message
string plainText = "Secret message";
string recipientPubKey = "recipient_public_key_hex";

var cipherResult = await cryptoService.AesEncrypt(
    plainText: plainText,
    theirPublicKey: recipientPubKey,
    ivOverride: null,  // Let the library generate a random IV
    ethereal: true     // Use the ethereal key pair
);

Console.WriteLine($"Encrypted: {cipherResult.CipherText}");
Console.WriteLine($"IV: {cipherResult.Iv}");
```

**Decrypting Text:**
```csharp
// Decrypt a message
string decrypted = await cryptoService.AesDecrypt(
    base64CipherText: cipherResult.CipherText,
    theirPublicKey: senderPubKey,
    ivString: cipherResult.Iv,
    ethereal: true
);

Console.WriteLine($"Decrypted: {decrypted}");
```

### Signing and Verifying Messages

**Sign a Message:**
```csharp
string message = "Message to sign";

// Create key pair first
cryptoService.CreateEtherealKeyPair();

// Sign the message
string signature = cryptoService.Sign(message, ethereal: true);

Console.WriteLine($"Signature: {signature}");
```

**Verify a Signature:**
```csharp
string signature = "signature_hex";
string message = "original_message";
string publicKey = "signer_public_key_hex";

bool isValid = cryptoService.Verify(signature, message, publicKey);

if (isValid)
{
    Console.WriteLine("Signature is valid!");
}
else
{
    Console.WriteLine("Invalid signature!");
}
```

### Verifying Event Signatures

Verify that a received event has a valid signature:

```csharp
var messageService = serviceProvider.GetRequiredService<IMessageService>();

// Assuming you have received an NEvent
bool isEventValid = messageService.Verify(receivedEvent);

if (isEventValid)
{
    Console.WriteLine("Event signature is valid!");
}
else
{
    Console.WriteLine("Invalid event signature - may be tampered!");
}
```

### Complete Example: Creating, Signing, and Sending an Event

```csharp
using BlazeJump.Tools;
using BlazeJump.Tools.Services.Crypto;
using BlazeJump.Tools.Services.Message;
using BlazeJump.Tools.Services.Connections;
using BlazeJump.Tools.Enums;
using Microsoft.Extensions.DependencyInjection;

public class CryptoExample
{
    public static async Task Main(string[] args)
    {
        // Setup DI
        var services = new ServiceCollection();
        services.ConfigureServices();
        var serviceProvider = services.BuildServiceProvider();
        
        // Get services
        var cryptoService = serviceProvider.GetRequiredService<ICryptoService>();
        var messageService = serviceProvider.GetRequiredService<IMessageService>();
        var relayManager = serviceProvider.GetRequiredService<IRelayManager>();
        
        // Step 1: Create a key pair
        cryptoService.CreateEtherealKeyPair();
        var pubKeyBytes = cryptoService.EtherealPublicKey?.ToXOnlyPubKey().ToBytes();
        var pubKeyHex = Convert.ToHexString(pubKeyBytes).ToLower();
        Console.WriteLine($"Your public key: {pubKeyHex}");
        
        // Step 2: Connect to relays
        relayManager.TryAddUri("wss://relay.damus.io");
        await relayManager.OpenConnection("wss://relay.damus.io");
        
        // Step 3: Create and send a public text note
        var textNote = messageService.CreateNEvent(
            kind: KindEnum.TextNote,
            message: "Hello from BlazeJump.Tools!",
            parentId: null,
            rootId: null,
            ptags: null
        );
        
        await messageService.Send(KindEnum.TextNote, textNote);
        Console.WriteLine($"Public note sent! Event ID: {textNote.Id}");
        
        // Step 4: Create and send an encrypted direct message
        string recipientPubKey = "recipient_hex_public_key"; // Replace with actual key
        
        var dmEvent = messageService.CreateNEvent(
            kind: KindEnum.EncryptedDirectMessage,
            message: "This is a private message!",
            parentId: null,
            rootId: null,
            ptags: new List<string> { recipientPubKey }
        );
        
        await messageService.Send(
            kind: KindEnum.EncryptedDirectMessage,
            nEvent: dmEvent,
            encryptPubKey: recipientPubKey
        );
        
        Console.WriteLine($"Encrypted DM sent! Event ID: {dmEvent.Id}");
        
        // Step 5: Verify an event signature
        bool isValid = messageService.Verify(textNote);
        Console.WriteLine($"Event signature valid: {isValid}");
    }
}
```

## Managing Connections

**Check Connected Relays:**
```csharp
var connectedRelays = _relayManager.Relays;
foreach (var relay in connectedRelays)
{
    Console.WriteLine($"Connected to: {relay}");
}
```

**Close a Connection:**
```csharp
await _relayManager.CloseConnection("wss://relay.damus.io");
```

**Check Connection Status:**
```csharp
if (_relayManager.RelayConnections.TryGetValue(relayUri, out var connection))
{
    bool isOpen = connection.IsOpen;
    Console.WriteLine($"Relay {relayUri} is {(isOpen ? "open" : "closed")}");
}
```

## Event Kinds Reference

Common Nostr event kinds available in `KindEnum`:

- `TextNote` (1) - Short text note
- `RecommendRelay` (2) - Recommend a relay
- `ContactList` (3) - Contact/follow list
- `EncryptedDirectMessage` (4) - Encrypted DM
- `EventDeletion` (5) - Delete event request
- `Reaction` (7) - Emoji reaction to another event
- `ChannelCreation` (40) - Create a channel
- `ChannelMetadata` (41) - Set channel metadata
- `ChannelMessage` (42) - Send message to channel

See the `KindEnum` class for the complete list of supported event kinds.

## Message Types

Available message types in `MessageTypeEnum`:

- `EVENT` - Regular event
- `REQ` - Request/subscription
- `CLOSE` - Close subscription
- `NOTICE` - Relay notice
- `EOSE` - End of stored events
- `OK` - Command result
- `AUTH` - Authentication challenge
- `COUNT` - Count query result

## Requirements

- .NET 9.0 or higher
- Compatible with Blazor WebAssembly, Blazor Server, and .NET MAUI

## Dependencies

This package includes the following dependencies:
- AutoMapper (13.0.1)
- Newtonsoft.Json (13.0.3)
- NBitcoin.Secp256k1 (3.1.4) - For cryptographic operations
- QRCoder (1.6.0) - For QR code generation
- Nano.Bech32 (1.0.0) - For Bech32 encoding/decoding

## License

This project is licensed under the MIT License.

## Support

For issues, questions, or contributions, please visit:
- GitHub Repository: [https://github.com/drmikesamy/BlazeJump](https://github.com/drmikesamy/BlazeJump)

## Additional Services

This package also includes:

- **IIdentityService** - Manage Nostr identities and key pairs
- **IMessageService** - Advanced message handling and validation
- **INotificationService** - Application notification management
- **IUserProfileService** - User profile operations and caching
- **Cryptographic Services** - Sign and verify Nostr events

Refer to the XML documentation in your IDE for detailed API documentation.