using System;
using System.Collections.Generic;
using System.Linq;
using CardanoSharp.Wallet.CIPs.CIP2;
using CardanoSharp.Wallet.CIPs.CIP2.ChangeCreationStrategies;
using CardanoSharp.Wallet.Extensions;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Transactions;
using Xunit;

namespace CardanoSharp.Wallet.Test.CIPs;

public partial class CIP2Tests
{
    [Fact]
    public void LargestFirst_Simple_Fee_Test()
    {
        //arrange
        var coinSelection = new CoinSelectionService(new LargestFirstStrategy(), new SingleTokenBundleStrategy());
        var outputs = new List<TransactionOutput>() { output_100_ada_no_assets };
        var utxos = new List<Utxo>() { utxo_50_ada_no_assets, utxo_50_ada_no_assets, utxo_10_ada_no_assets, utxo_20_ada_no_assets, };

        //act
        ulong feeBuffer = 21 * adaToLovelace;
        var response = coinSelection.GetCoinSelection(outputs, utxos, address, feeBuffer: feeBuffer);

        //assert
        Assert.Equal(utxo_50_ada_no_assets.TxHash, response.SelectedUtxos[0].TxHash);
        Assert.Equal(utxo_50_ada_no_assets.TxHash.HexToByteArray(), response.Inputs[0].TransactionId);
        Assert.Equal(utxo_50_ada_no_assets.TxHash, response.SelectedUtxos[1].TxHash);
        Assert.Equal(utxo_50_ada_no_assets.TxHash.HexToByteArray(), response.Inputs[1].TransactionId);
        Assert.Equal(utxo_20_ada_no_assets.TxHash, response.SelectedUtxos[2].TxHash);
        Assert.Equal(utxo_20_ada_no_assets.TxHash.HexToByteArray(), response.Inputs[2].TransactionId);
        Assert.Equal(utxo_10_ada_no_assets.TxHash, response.SelectedUtxos[3].TxHash);
        Assert.Equal(utxo_10_ada_no_assets.TxHash.HexToByteArray(), response.Inputs[3].TransactionId);

        long totalSelected = 0;
        response.SelectedUtxos.ForEach(s => totalSelected = totalSelected + (long)s.Balance.Lovelaces);
        long totalOutput = 0;
        outputs.ForEach(o => totalOutput = totalOutput + (long)o.Value.Coin);
        long totalChange = 0;
        response.ChangeOutputs.ForEach(s => totalChange = totalChange + (long)s.Value.Coin);
        long finalChangeOutputChange = (long)response.ChangeOutputs.Last().Value.Coin;
        Assert.Equal(totalSelected, totalOutput + totalChange);
        Assert.True((ulong)finalChangeOutputChange >= feeBuffer);
    }

    [Fact]
    public void LargestFirst_Simple_Fee_Fail_Test()
    {
        //arrange
        var coinSelection = new CoinSelectionService(new LargestFirstStrategy(), new SingleTokenBundleStrategy());
        var outputs = new List<TransactionOutput>() { output_100_ada_no_assets };
        var utxos = new List<Utxo>() { utxo_50_ada_no_assets, utxo_50_ada_no_assets, utxo_10_ada_no_assets, };

        //assert
        try
        {
            //act
            var response = coinSelection.GetCoinSelection(outputs, utxos, address, feeBuffer: 11 * adaToLovelace);
        }
        catch (Exception e)
        {
            //assert
            Assert.Equal("UTxOs have insufficient balance", e.Message);
        }
    }

    [Fact]
    public void LargestFirst_BasicChange_Fee_Test()
    {
        //arrange
        var coinSelection = new CoinSelectionService(new LargestFirstStrategy(), new BasicChangeSelectionStrategy());
        var outputs = new List<TransactionOutput>() { output_10_ada_no_assets, output_10_ada_no_assets, output_10_ada_no_assets };
        var utxos = new List<Utxo>()
        {
            utxo_10_ada_10_tokens,
            utxo_10_ada_1_owned_mint_asset,
            utxo_10_ada_1_owned_mint_asset_two,
            utxo_10_ada_100_owned_mint_asset,
        };

        //act
        ulong feeBuffer = 3 * adaToLovelace;
        var response = coinSelection.GetCoinSelection(outputs, utxos, address, feeBuffer: feeBuffer);

        //assert
        Assert.Equal(utxo_10_ada_10_tokens.TxHash, response.SelectedUtxos[0].TxHash);
        Assert.Equal(utxo_10_ada_10_tokens.TxHash.HexToByteArray(), response.Inputs[0].TransactionId);
        Assert.Equal(utxo_10_ada_1_owned_mint_asset.TxHash, response.SelectedUtxos[1].TxHash);
        Assert.Equal(utxo_10_ada_1_owned_mint_asset.TxHash.HexToByteArray(), response.Inputs[1].TransactionId);
        Assert.Equal(utxo_10_ada_1_owned_mint_asset_two.TxHash, response.SelectedUtxos[2].TxHash);
        Assert.Equal(utxo_10_ada_1_owned_mint_asset_two.TxHash.HexToByteArray(), response.Inputs[2].TransactionId);
        Assert.Equal(utxo_10_ada_100_owned_mint_asset.TxHash, response.SelectedUtxos[3].TxHash);
        Assert.Equal(utxo_10_ada_100_owned_mint_asset.TxHash.HexToByteArray(), response.Inputs[3].TransactionId);
        Assert.Equal(4, response.SelectedUtxos.Count);
        Assert.Equal(1, response.ChangeOutputs.Count);

        long totalSelected = 0;
        response.SelectedUtxos.ForEach(s => totalSelected = totalSelected + (long)s.Balance.Lovelaces);
        long totalOutput = 0;
        outputs.ForEach(o => totalOutput = totalOutput + (long)o.Value.Coin);
        long totalChange = 0;
        response.ChangeOutputs.ForEach(s => totalChange = totalChange + (long)s.Value.Coin);
        long finalChangeOutputChange = (long)response.ChangeOutputs.Last().Value.Coin;
        Assert.Equal(totalSelected, totalOutput + totalChange);
        Assert.True((ulong)finalChangeOutputChange >= feeBuffer);
    }

    [Fact]
    public void LargestFirst_BasicChange_Fee_Test_2()
    {
        //arrange
        var coinSelection = new CoinSelectionService(new LargestFirstStrategy(), new BasicChangeSelectionStrategy());
        var outputs = new List<TransactionOutput>() { output_10_ada_no_assets, output_10_ada_no_assets, output_10_ada_no_assets };
        var utxos = new List<Utxo>()
        {
            utxo_10_ada_10_tokens,
            utxo_10_ada_1_owned_mint_asset,
            utxo_10_ada_1_owned_mint_asset_two,
            utxo_10_ada_100_owned_mint_asset,
            utxo_10_ada_100_owned_mint_asset_two,
        };

        //act
        ulong feeBuffer = 11 * adaToLovelace;
        var response = coinSelection.GetCoinSelection(outputs, utxos, address, feeBuffer: 11 * adaToLovelace);

        //assert
        Assert.Equal(utxo_10_ada_10_tokens.TxHash, response.SelectedUtxos[0].TxHash);
        Assert.Equal(utxo_10_ada_10_tokens.TxHash.HexToByteArray(), response.Inputs[0].TransactionId);
        Assert.Equal(utxo_10_ada_1_owned_mint_asset.TxHash, response.SelectedUtxos[1].TxHash);
        Assert.Equal(utxo_10_ada_1_owned_mint_asset.TxHash.HexToByteArray(), response.Inputs[1].TransactionId);
        Assert.Equal(utxo_10_ada_1_owned_mint_asset_two.TxHash, response.SelectedUtxos[2].TxHash);
        Assert.Equal(utxo_10_ada_1_owned_mint_asset_two.TxHash.HexToByteArray(), response.Inputs[2].TransactionId);
        Assert.Equal(utxo_10_ada_100_owned_mint_asset.TxHash, response.SelectedUtxos[3].TxHash);
        Assert.Equal(utxo_10_ada_100_owned_mint_asset.TxHash.HexToByteArray(), response.Inputs[3].TransactionId);
        Assert.Equal(utxo_10_ada_100_owned_mint_asset_two.TxHash, response.SelectedUtxos[4].TxHash);
        Assert.Equal(utxo_10_ada_100_owned_mint_asset_two.TxHash.HexToByteArray(), response.Inputs[4].TransactionId);
        Assert.Equal(5, response.SelectedUtxos.Count);
        Assert.Equal(1, response.ChangeOutputs.Count);
        Assert.Equal(5, response.ChangeOutputs.First().Value.MultiAsset.Count);

        long totalSelected = 0;
        response.SelectedUtxos.ForEach(s => totalSelected = totalSelected + (long)s.Balance.Lovelaces);
        long totalOutput = 0;
        outputs.ForEach(o => totalOutput = totalOutput + (long)o.Value.Coin);
        long totalChange = 0;
        response.ChangeOutputs.ForEach(s => totalChange = totalChange + (long)s.Value.Coin);
        long finalChangeOutputChange = (long)response.ChangeOutputs.Last().Value.Coin;
        Assert.Equal(totalSelected, totalOutput + totalChange);
        Assert.True((ulong)finalChangeOutputChange >= feeBuffer);
    }

    [Fact]
    public void LargestFirst_BasicChange_Fee_Fail_Test()
    {
        //arrange
        var coinSelection = new CoinSelectionService(new LargestFirstStrategy(), new BasicChangeSelectionStrategy());
        var outputs = new List<TransactionOutput>() { output_100_ada_no_assets };
        var utxos = new List<Utxo>() { utxo_50_ada_no_assets, utxo_50_ada_no_assets, utxo_10_ada_no_assets, };

        //assert
        try
        {
            //act
            var response = coinSelection.GetCoinSelection(outputs, utxos, address, feeBuffer: 11 * adaToLovelace);
        }
        catch (Exception e)
        {
            //assert
            Assert.Equal("UTxOs have insufficient balance", e.Message);
        }
    }
}
