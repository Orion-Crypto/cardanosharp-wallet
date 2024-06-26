﻿using System;
using System.Collections.Generic;
using System.Linq;
using CardanoSharp.Wallet.CIPs.CIP2.ChangeCreationStrategies;
using CardanoSharp.Wallet.CIPs.CIP2.Extensions;
using CardanoSharp.Wallet.CIPs.CIP2.Models;
using CardanoSharp.Wallet.Common;
using CardanoSharp.Wallet.Extensions;
using CardanoSharp.Wallet.Extensions.Models.Certificates;
using CardanoSharp.Wallet.Extensions.Models.Transactions;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Addresses;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.TransactionBuilding;

namespace CardanoSharp.Wallet.CIPs.CIP2;

public interface ICoinSelectionService
{
    CoinSelection GetCoinSelection(
        List<TransactionOutput> outputs,
        List<Utxo> utxos,
        string changeAddress,
        ITokenBundleBuilder? mint = null,
        List<ICertificateBuilder>? certificates = null,
        List<Utxo>? requiredUtxos = null,
        int limit = 20,
        ulong feeBuffer = 0
    );
}

public class CoinSelectionService : ICoinSelectionService
{
    private readonly ICoinSelectionStrategy _coinSelection;
    private readonly IChangeCreationStrategy _changeCreation;

    public CoinSelectionService(ICoinSelectionStrategy coinSelection, IChangeCreationStrategy changeCreation)
    {
        _coinSelection = coinSelection;
        _changeCreation = changeCreation;
    }

    public CoinSelection GetCoinSelection(
        List<TransactionOutput> outputs,
        List<Utxo> utxos,
        string changeAddress,
        ITokenBundleBuilder? mint = null,
        List<ICertificateBuilder>? certificates = null,
        List<Utxo>? requiredUtxos = null,
        int limit = 20,
        ulong feeBuffer = 0
    )
    {
        var protocolParameters = new ProtocolParameters();
        var coinSelection = new CoinSelection();
        var availableUTxOs = new List<Utxo>(utxos);

        // Add Required UTXOs to selection
        _coinSelection.SelectRequiredInputs(coinSelection, requiredUtxos);

        // Get the balance of all the outputs we must find inputs for taking into account mint and feeBuffers
        var balance = outputs.AggregateAssets(protocolParameters, mint, certificates, feeBuffer);

        // Perform initial selection of multi assets and ada
        foreach (var asset in balance.Assets)
        {
            _coinSelection.SelectInputs(coinSelection, availableUTxOs, asset.Quantity, asset, requiredUtxos, limit);

            if (!HasSufficientBalance(coinSelection.SelectedUtxos, asset.Quantity, asset))
                throw new Exception("UTxOs have insufficient balance");
        }
        _coinSelection.SelectInputs(coinSelection, availableUTxOs, (long)balance.Lovelaces, null, requiredUtxos, limit);
        if (!HasSufficientBalance(coinSelection.SelectedUtxos, (long)balance.Lovelaces, null))
            throw new Exception("UTxOs have insufficient balance");

        // We need to determine if we have any change for tokens. this way we can accommodate the min lovelaces in our current value
        if (coinSelection.SelectedUtxos.Any() && _changeCreation is not null)
            _changeCreation.CalculateChange(coinSelection, balance, changeAddress, feeBuffer: feeBuffer);

        // Calculate change ada from ouputs (not the current change in the coinSelection) and the minium required change ada
        long change = CalculateChangeADA(coinSelection, balance, feeBuffer);
        long minChangeAdaRequired = CalculateMinChangeADARequired(coinSelection, feeBuffer);

        // Perform additional input selection until we have enough ada to cover the new min amounts (since they change each selction) or we run out of inputs
        while (change < minChangeAdaRequired && availableUTxOs.Count > 0)
        {
            // FeeBuffer is already in this calculation from minChangeAdaRequired
            long minADA = minChangeAdaRequired - change + coinSelection.SelectedUtxos.Select(x => (long)x.Balance.Lovelaces).Sum();

            _coinSelection.SelectInputs(coinSelection, availableUTxOs, minADA, null, requiredUtxos, limit);
            if (!HasSufficientBalance(coinSelection.SelectedUtxos, minADA, null))
                throw new Exception("UTxOs have insufficient balance");

            _changeCreation?.CalculateChange(coinSelection, balance, changeAddress, feeBuffer: feeBuffer);

            change = CalculateChangeADA(coinSelection, balance, feeBuffer);
            minChangeAdaRequired = CalculateMinChangeADARequired(coinSelection, feeBuffer);
        }

        // Final check to ensure we have a valid transaction
        if (change < minChangeAdaRequired && availableUTxOs.Count <= 0)
            throw new Exception("UTxOs have insufficient balance");

        PopulateInputList(coinSelection);

        return coinSelection;
    }

    private static bool HasSufficientBalance(IEnumerable<Utxo> selectedUtxos, long amount, Asset? asset = null)
    {
        long totalInput = 0;
        foreach (var su in selectedUtxos)
        {
            long quantity = 0;
            if (asset is null)
            {
                quantity = (long)su.Balance.Lovelaces;
            }
            else
            {
                quantity =
                    su.Balance.Assets.FirstOrDefault(ma => ma.PolicyId.SequenceEqual(asset.PolicyId) && ma.Name.Equals(asset.Name))?.Quantity ?? 0;
            }

            totalInput += quantity;
        }

        return totalInput >= amount;
    }

    private static long CalculateChangeADA(CoinSelection coinSelection, Balance balance, ulong feeBuffer = 0)
    {
        long inputADA = coinSelection.SelectedUtxos.Select(x => (long)x.Balance.Lovelaces).Sum();
        long outputADA = (long)balance.Lovelaces - (long)feeBuffer; // Subtract feebuffer to get the real outputADA amount
        long change = inputADA - outputADA;
        if (change <= 0)
            change = 0;
        return change;
    }

    private static long CalculateMinChangeADARequired(CoinSelection coinSelection, ulong feeBuffer = 0)
    {
        long minChangeADARequired = (long)feeBuffer; // Ensure we have enough ada equal to the min required + fee buffer
        foreach (var changeOutput in coinSelection.ChangeOutputs)
        {
            minChangeADARequired += (long)changeOutput.CalculateMinUtxoLovelace();
        }
        return minChangeADARequired;
    }

    private static void PopulateInputList(CoinSelection coinSelection)
    {
        foreach (var su in coinSelection.SelectedUtxos)
        {
            coinSelection.Inputs.Add(
                new TransactionInput()
                {
                    TransactionId = su.TxHash.HexToByteArray(),
                    TransactionIndex = su.TxIndex,
                    Output =
                        su.OutputAddress != null
                            ? TransactionOutputBuilder.Create
                                .SetOutputFromUtxo(new Address(su.OutputAddress).GetBytes(), su, su.OutputDatumOption, su.OutputScriptReference)
                                .Build()
                            : null
                }
            );
        }
    }
}
