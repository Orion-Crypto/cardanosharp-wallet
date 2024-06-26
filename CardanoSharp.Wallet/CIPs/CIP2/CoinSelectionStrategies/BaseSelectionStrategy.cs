﻿using System.Collections.Generic;
using System.Linq;
using CardanoSharp.Wallet.CIPs.CIP2.Models;
using CardanoSharp.Wallet.Extensions;
using CardanoSharp.Wallet.Extensions.Models.Transactions;
using CardanoSharp.Wallet.Models;

namespace CardanoSharp.Wallet.CIPs.CIP2;

public abstract class BaseSelectionStrategy
{
    //---------------------------------------------------------------------------------------------------//
    // Balance Functions
    //---------------------------------------------------------------------------------------------------//
    protected static long GetCurrentBalance(CoinSelection coinSelection, Asset? asset = null)
    {
        if (asset is null)
        {
            ulong minLovelaces = 0;
            if (coinSelection.ChangeOutputs.Any())
            {
                minLovelaces = coinSelection.ChangeOutputs.First().CalculateMinUtxoLovelace();
                coinSelection.ChangeOutputs.First().Value.Coin = minLovelaces;
            }
            return coinSelection.SelectedUtxos.Sum(x => (long)x.Balance.Lovelaces) - (long)minLovelaces;
        }
        else
        {
            return coinSelection.SelectedUtxos
                .Where(x => x.Balance.Assets is not null)
                .Sum(
                    x =>
                        (long)(
                            x.Balance.Assets.FirstOrDefault(ma => ma.PolicyId.SequenceEqual(asset.PolicyId) && ma.Name.Equals(asset.Name))?.Quantity
                            ?? 0
                        )
                );
        }
    }

    //---------------------------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------------------------//
    // Utxo Functions
    //---------------------------------------------------------------------------------------------------//
    public static void SelectRequiredUtxos(CoinSelection coinSelection, List<Utxo>? requiredUtxos)
    {
        if (requiredUtxos == null)
            return;

        foreach (var utxo in requiredUtxos)
            coinSelection.SelectedUtxos.Add(utxo);
    }

    protected static List<Utxo> OrderUtxosByDescending(List<Utxo> utxos, Asset? asset = null)
    {
        var orderedUtxos = new List<Utxo>();
        if (asset is null)
            orderedUtxos = utxos.OrderByDescending(x => x.Balance.Lovelaces).ToList();
        else
        {
            orderedUtxos = utxos
                .Where(
                    x =>
                        x.Balance.Assets is not null
                        && x.Balance.Assets.FirstOrDefault(ma => ma.PolicyId.SequenceEqual(asset.PolicyId) && ma.Name.Equals(asset.Name)) is not null
                )
                .OrderByDescending(
                    x => x.Balance.Assets.FirstOrDefault(ma => ma.PolicyId.SequenceEqual(asset.PolicyId) && ma.Name.Equals(asset.Name))!.Quantity
                )
                .ToList();
        }

        return orderedUtxos;
    }

    protected static List<Utxo> OrderUtxosByAscending(List<Utxo> utxos, Asset? asset = null)
    {
        var orderedUtxos = new List<Utxo>();
        if (asset is null)
            orderedUtxos = utxos.OrderBy(x => x.Balance.Lovelaces).ToList();
        else
        {
            orderedUtxos = utxos
                .Where(
                    x =>
                        x.Balance.Assets is not null
                        && x.Balance.Assets.FirstOrDefault(ma => ma.PolicyId.SequenceEqual(asset.PolicyId) && ma.Name.Equals(asset.Name)) is not null
                )
                .OrderBy(x => x.Balance.Assets.First(ma => ma.PolicyId.SequenceEqual(asset.PolicyId) && ma.Name.Equals(asset.Name)).Quantity)
                .ToList();
        }

        return orderedUtxos;
    }

    protected static List<Utxo> FilterUtxosByAsset(List<Utxo> utxos, Asset? asset = null)
    {
        var filteredUtxos = new List<Utxo>();
        if (asset is null)
            return utxos;

        filteredUtxos = utxos
            .Where(
                x =>
                    x.Balance.Assets is not null
                    && x.Balance.Assets.FirstOrDefault(ma => ma.PolicyId.SequenceEqual(asset.PolicyId) && ma.Name.Equals(asset.Name)) is not null
            )
            .ToList();
        return filteredUtxos;
    }
    //---------------------------------------------------------------------------------------------------//
}
