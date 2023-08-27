using System.Collections.Generic;
using System.Threading.Tasks;
using CardanoSharp.Blockfrost.Sdk;
using CardanoSharp.Blockfrost.Sdk.Contracts;
using CardanoSharp.Wallet.Enums;
using CardanoSharp.Wallet.Models;

namespace CardanoSharp.Wallet.Providers;

public interface IAProviderService
{
    Task Initialize(NetworkType networkType = NetworkType.Mainnet);

    //---------------------------------------------------------------------------------------------------//
    // Account Functions
    //---------------------------------------------------------------------------------------------------//
    public Task<string?> GetMainAddress(string? address, string order = "desc");

    //---------------------------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------------------------//
    // Address Functions
    //---------------------------------------------------------------------------------------------------//
    public Task<List<Utxo>> GetSingleAddressUtxos(string address);
    public Task<List<Utxo>> GetUtxos(string address, bool filterSmartContractAddresses = false);

    //---------------------------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------------------------//
    // Mempool Functions
    //---------------------------------------------------------------------------------------------------//
    Task<MempoolTransaction[]> GetMempoolTransactions(List<string> txHash);
    //---------------------------------------------------------------------------------------------------//
}

public class ProviderData
{
    public NetworkType NetworkType { get; set; } = NetworkType.Mainnet;
    public Block? Block { get; set; } = default!;
    public EpochParameters? ProtocolParameters { get; set; } = default!;
}

public abstract class AProviderService : IAProviderService
{
    public IAccountClient AccountClient { get; set; } = default!;
    public IAddressesClient AddressesClient { get; set; } = default!;
    public IAssetsClient AssetsClient { get; set; } = default!;
    public IBlocksClient BlocksClient { get; set; } = default!;
    public IEpochsClient EpochsClient { get; set; } = default!;
    public IMempoolClient MempoolClient { get; set; } = default!;
    public INetworkClient NetworkClient { get; set; } = default!;
    public IPoolsClient PoolsClient { get; set; } = default!;
    public IScriptsClient ScriptsClient { get; set; } = default!;
    public ITransactionsClient TransactionsClient { get; set; } = default!;

    // Provider Data
    public ProviderData ProviderData { get; set; } = default!;

    public virtual async Task Initialize(NetworkType networkType = NetworkType.Mainnet)
    {
        this.ProviderData.NetworkType = networkType;
        this.ProviderData.Block = (await BlocksClient.GetLatestBlockAsync())?.Content!;
        this.ProviderData.ProtocolParameters = (await EpochsClient.GetLatestParamtersAsync())?.Content!;
    }

    //---------------------------------------------------------------------------------------------------//
    // Account Functions
    //---------------------------------------------------------------------------------------------------//
    public virtual Task<string?> GetMainAddress(string? address, string order = "desc")
    {
        throw new System.NotImplementedException();
    }

    //---------------------------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------------------------//
    // Address Functions
    //---------------------------------------------------------------------------------------------------//
    public virtual Task<List<Utxo>> GetSingleAddressUtxos(string address)
    {
        throw new System.NotImplementedException();
    }

    public virtual Task<List<Utxo>> GetUtxos(string address, bool filterSmartContractAddresses = false)
    {
        throw new System.NotImplementedException();
    }

    //---------------------------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------------------------//
    // Mempool Functions
    //---------------------------------------------------------------------------------------------------//
    public virtual Task<MempoolTransaction[]> GetMempoolTransactions(List<string> txHash)
    {
        throw new System.NotImplementedException();
    }
    //---------------------------------------------------------------------------------------------------//
}
