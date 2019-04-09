﻿using VRageMath;
using System;
using DefenseShields.Support;
using Sandbox.ModAPI;
using VRage;

namespace DefenseShields
{
    public partial class DefenseShields
    {
        #region Block Power Logic
        private bool PowerOnline()
        {
            UpdateGridPower();
            if (!_shieldPowered) return false;

            CalculatePowerCharge();

            if (!WarmedUp) return true;
            if (_isServer && _shieldConsumptionRate.Equals(0f) && DsState.State.Charge.Equals(0.01f))
            {
                return false;
            }

            _power = _shieldMaxChargeRate > 0 ? _shieldConsumptionRate + _shieldMaintaintPower : 0f;
            if (_power < ShieldCurrentPower && (_power - _shieldMaxChargeRate) >= 0.0001f) //overpower
                _sink.Update();
            else if (_count == 28 && (ShieldCurrentPower <= 0 || Math.Abs(_power - ShieldCurrentPower) >= 0.0001f))
                _sink.Update();

            if (Absorb > 0)
            {
                _damageReadOut += Absorb;
                EffectsCleanTick = _tick;
                DsState.State.Charge -= Absorb * ConvToWatts;
            }
            else if (Absorb < 0) DsState.State.Charge += Absorb * ConvToWatts;

            if (_isServer && DsState.State.Charge < 0)
            {
                DsState.State.Charge = 0;
                if (!_empOverLoad) _overLoadLoop = 0;
                else _empOverLoadLoop = 0;
            }
            Absorb = 0f;
            return true;
        }

        private void UpdateGridPower()
        {
            GridAvailablePower = 0;
            GridMaxPower = 0;
            GridCurrentPower = 0;
            _batteryMaxPower = 0;
            _batteryCurrentOutput = 0;
            _batteryCurrentInput = 0;
            lock (SubLock)
            {
                if (MyResourceDist != null && FuncTask.IsComplete && !_functionalEvent)
                {
                    var noObjects = MyResourceDist.SourcesEnabled == MyMultipleEnabledEnum.NoObjects;
                    if (noObjects)
                    {
                        if (Session.Enforced.Debug >= 2) Log.Line($"NoObjects: {MyGrid?.DebugName} - Max:{MyResourceDist?.MaxAvailableResourceByType(GId)} - Status:{MyResourceDist?.SourcesEnabled} - Sources:{_powerSources.Count}");
                        FallBackPowerCalc();
                        FunctionalChanged(true);
                    }
                    else
                    {
                        GridMaxPower = MyResourceDist.MaxAvailableResourceByType(GId);
                        GridCurrentPower = MyResourceDist.TotalRequiredInputByType(GId);
                        if (!DsSet.Settings.UseBatteries && _batteryBlocks.Count != 0) CalculateBatteryInput();
                    }
                }
                else FallBackPowerCalc();
            }
            GridAvailablePower = GridMaxPower - GridCurrentPower;

            if (!DsSet.Settings.UseBatteries)
            {
                GridCurrentPower += _batteryCurrentInput;
                GridAvailablePower -= _batteryCurrentInput;
            }

            var reserveScaler = ReserveScaler[DsSet.Settings.PowerScale];
            var userPowerCap = DsSet.Settings.PowerWatts * reserveScaler;
            var shieldMax = GridMaxPower > userPowerCap ? userPowerCap : GridMaxPower;
            ShieldMaxPower = shieldMax;
            ShieldAvailablePower = ShieldMaxPower - GridCurrentPower;
            _shieldPowered = ShieldMaxPower > 0;
        }

        private void FallBackPowerCalc(bool reportOnly = false)
        {
            var batteries = !DsSet.Settings.UseBatteries;
            if (reportOnly)
            {
                var gridMaxPowerReport = 0f;
                var gridCurrentPowerReport = 0f;
                var gridAvailablePowerReport = 0f;
                var batteryMaxPowerReport = 0f;
                var batteryCurrentPowerReport = 0f;
                var batteryCurrentInputreport = 0f;
                for (int i = 0; i < _powerSources.Count; i++)
                {
                    var source = _powerSources[i];

                    var battery = source.Entity as IMyBatteryBlock;
                    if (battery != null && batteries)
                    {
                        //Log.Line($"bMaxO:{battery.MaxOutput} - bCurrO:{battery.CurrentOutput} - bCurrI:{battery.CurrentInput} - Charging:{battery.IsCharging}");
                        if (!battery.IsWorking) continue;
                        var currentInput = battery.CurrentInput;
                        var currentOutput = battery.CurrentOutput;
                        var maxOutput = battery.MaxOutput;
                        if (currentInput > 0)
                        {
                            batteryCurrentInputreport += currentInput;
                            if (battery.IsCharging) batteryCurrentPowerReport -= currentInput;
                            else batteryCurrentPowerReport -= currentInput;
                        }
                        batteryMaxPowerReport += maxOutput;
                        batteryCurrentPowerReport += currentOutput;
                    }
                    else
                    {
                        gridMaxPowerReport += source.MaxOutputByType(GId);
                        gridCurrentPowerReport += source.CurrentOutputByType(GId);
                    }
                }

                gridMaxPowerReport += batteryMaxPowerReport;
                gridCurrentPowerReport += batteryCurrentPowerReport;
                gridAvailablePowerReport = gridMaxPowerReport - gridCurrentPowerReport;

                if (!DsSet.Settings.UseBatteries)
                {
                    gridCurrentPowerReport += batteryCurrentInputreport;
                    gridAvailablePowerReport -= batteryCurrentInputreport;
                }

                Log.Line($"Report: PriGMax:{GridMaxPower}(BetaGMax:{gridMaxPowerReport}) - PriGCurr:{GridCurrentPower}(BetaGCurr:{gridCurrentPowerReport}) - PriGAvail:{GridMaxPower - GridCurrentPower}(BetaGAvail:{gridAvailablePowerReport}) - BatInput:{batteryCurrentInputreport} - SCurr:{ShieldCurrentPower}");
            }
            else
            {
                for (int i = 0; i < _powerSources.Count; i++)
                {
                    var source = _powerSources[i];
                    var battery = source.Entity as IMyBatteryBlock;
                    if (battery != null && batteries)
                    {
                        //Log.Line($"bMaxO:{battery.MaxOutput} - bCurrO:{battery.CurrentOutput} - bCurrI:{battery.CurrentInput} - Charging:{battery.IsCharging}");
                        if (!battery.IsWorking) continue;
                        var currentInput = battery.CurrentInput;
                        var currentOutput = battery.CurrentOutput;
                        var maxOutput = battery.MaxOutput;
                        if (currentInput > 0)
                        {
                            _batteryCurrentInput += currentInput;
                            if (battery.IsCharging) _batteryCurrentOutput -= currentInput;
                            else _batteryCurrentOutput -= currentInput;
                        }
                        _batteryMaxPower += maxOutput;
                        _batteryCurrentOutput += currentOutput;
                    }
                    else
                    {
                        GridMaxPower += source.MaxOutputByType(GId);
                        GridCurrentPower += source.CurrentOutputByType(GId);
                    }
                }
                GridMaxPower += _batteryMaxPower;
                GridCurrentPower += _batteryCurrentOutput;
            }
        }

        private void CalculateBatteryInput()
        {
            for (int i = 0; i < _batteryBlocks.Count; i++)
            {
                var battery = _batteryBlocks[i];
                if (!battery.IsWorking) continue;
                var currentInput = battery.CurrentInput;
                var currentOutput = battery.CurrentOutput;
                var maxOutput = battery.MaxOutput;
                if (currentInput > 0)
                {
                    _batteryCurrentInput += currentInput;
                    if (battery.IsCharging) _batteryCurrentOutput -= currentInput;
                    else _batteryCurrentOutput -= currentInput;
                }
                _batteryMaxPower += maxOutput;
                _batteryCurrentOutput += currentOutput;
            }
        }

        private void CalculatePowerCharge()
        {
            var capScaler = Session.Enforced.CapScaler;
            var hpsEfficiency = Session.Enforced.HpsEfficiency;
            var baseScaler = Session.Enforced.BaseScaler;
            var maintenanceCost = Session.Enforced.MaintenanceCost;

            var percent = DsSet.Settings.Rate * ChargeRatio;

            var chargePercent = DsSet.Settings.Rate * ConvToDec;
            var shieldMaintainPercent = maintenanceCost / percent;
            _sizeScaler = _shieldVol / (_ellipsoidSurfaceArea * MagicRatio);

            float bufferScaler;
            if (ShieldMode == ShieldType.Station)
            {
                if (DsState.State.Enhancer) bufferScaler = 100 / percent * baseScaler * _shieldRatio;
                else bufferScaler = 100 / percent * baseScaler / (float)_sizeScaler * _shieldRatio;
            }
            else if (_sizeScaler > 1 && DsSet.Settings.FortifyShield)
            {
                bufferScaler = 100 / percent * baseScaler * _shieldRatio;
            }
            else bufferScaler = 100 / percent * baseScaler / (float)_sizeScaler * _shieldRatio;

            ShieldHpBase = ShieldMaxPower * bufferScaler;

            var gridIntegrity = DsState.State.GridIntegrity * ConvToDec;
            if (capScaler > 0) gridIntegrity *= capScaler;

            if (ShieldHpBase > gridIntegrity) HpScaler = gridIntegrity / ShieldHpBase;
            else HpScaler = 1f;

            shieldMaintainPercent = shieldMaintainPercent * DsState.State.EnhancerPowerMulti * (DsState.State.ShieldPercent * ConvToDec);
            if (DsState.State.Lowered) shieldMaintainPercent = shieldMaintainPercent * 0.25f;
            _shieldMaintaintPower = ShieldMaxPower * HpScaler * shieldMaintainPercent;

            ShieldMaxCharge = ShieldHpBase * HpScaler;
            var powerForShield = PowerNeeded(chargePercent, hpsEfficiency);
            if (!WarmedUp) return;

            if (DsState.State.Charge > ShieldMaxCharge) DsState.State.Charge = ShieldMaxCharge;
            if (_isServer)
            {
                var powerLost = powerForShield <= 0 || _powerNeeded > ShieldMaxPower || (ShieldMaxPower - _powerNeeded) / Math.Abs(_powerNeeded) * 100 < 0.001;
                var serverNoPower = DsState.State.NoPower;
                if (powerLost || serverNoPower)
                {
                    if (PowerLoss(powerForShield, powerLost, serverNoPower))
                    {
                        _powerFail = true;
                        return;
                    }
                }
                else
                {
                    if (_capacitorLoop != 0 && _tick - _capacitorTick > CapacitorStableCount)
                    {
                        _capacitorLoop = 0;
                    }
                    _powerFail = false;
                }
            }
            if (DsState.State.Heat != 0) UpdateHeatRate();
            else _expChargeReduction = 0;

            if (_count == 29 && DsState.State.Charge < ShieldMaxCharge) DsState.State.Charge += ShieldChargeRate;
            else if (DsState.State.Charge.Equals(ShieldMaxCharge))
            {
                ShieldChargeRate = 0f;
                _shieldConsumptionRate = 0f;
            }

            if (DsState.State.Charge < ShieldMaxCharge) DsState.State.ShieldPercent = DsState.State.Charge / ShieldMaxCharge * 100;
            else if (DsState.State.Charge < ShieldMaxCharge * 0.1) DsState.State.ShieldPercent = 0f;
            else DsState.State.ShieldPercent = 100f;
        }

        private float PowerNeeded(float chargePercent, float hpsEfficiency)
        {
            var powerScaler = 1f;
            if (HpScaler < 1) powerScaler = HpScaler;
            var cleanPower = ShieldAvailablePower + ShieldCurrentPower;
            _otherPower = ShieldMaxPower - cleanPower;
            var powerForShield = ((cleanPower * chargePercent) - _shieldMaintaintPower) * powerScaler;
            var rawMaxChargeRate = powerForShield > 0 ? powerForShield : 0f;
            _shieldMaxChargeRate = rawMaxChargeRate;
            _shieldPeakRate = _shieldMaxChargeRate * hpsEfficiency / (float)_sizeScaler;

            if (DsState.State.Charge + _shieldPeakRate < ShieldMaxCharge)
            {
                ShieldChargeRate = _shieldPeakRate;
                _shieldConsumptionRate = _shieldMaxChargeRate;
            }
            else
            {
                if (_shieldPeakRate > 0)
                {
                    var remaining = MathHelper.Clamp(ShieldMaxCharge - DsState.State.Charge, 0, ShieldMaxCharge);
                    var remainingScaled = remaining / _shieldPeakRate;
                    _shieldConsumptionRate = remainingScaled * _shieldMaxChargeRate;
                    ShieldChargeRate = _shieldPeakRate * remainingScaled;
                }
                else
                {
                    _shieldConsumptionRate = 0;
                    ShieldChargeRate = 0;
                }
            }
            _powerNeeded = _shieldMaintaintPower + _shieldConsumptionRate + _otherPower;
            return powerForShield;
        }

        private bool PowerLoss(float powerForShield, bool powerLost, bool serverNoPower)
        {
            if (powerLost)
            {
                if (!DsState.State.Online)
                {
                    DsState.State.Charge = 0.01f;
                    ShieldChargeRate = 0f;
                    _shieldConsumptionRate = 0f;
                    return true;
                }

                _capacitorTick = _tick;
                _capacitorLoop++;
                if (_capacitorLoop > CapacitorDrainCount)
                {
                    if (Session.Enforced.Debug >= 3 && _tick60) Log.Line($"CapcitorDrained");
                    if (!DsState.State.NoPower)
                    {
                        DsState.State.NoPower = true;
                        DsState.State.Message = true;
                        if (Session.Enforced.Debug >= 3) Log.Line($"StateUpdate: NoPower - forShield:{powerForShield} - max:{ShieldMaxPower} - avail{ShieldAvailablePower} - sCurr:{ShieldCurrentPower} - count:{_powerSources.Count} - DistEna:{MyResourceDist?.SourcesEnabled} - State:{MyResourceDist?.ResourceState} - ShieldId [{Shield.EntityId}]");
                        ShieldChangeState();
                    }

                    var shieldLoss = ShieldMaxCharge * 0.0016667f;
                    DsState.State.Charge = DsState.State.Charge - shieldLoss;
                    if (DsState.State.Charge < 0.01f) DsState.State.Charge = 0.01f;

                    if (DsState.State.Charge < ShieldMaxCharge) DsState.State.ShieldPercent = DsState.State.Charge / ShieldMaxCharge * 100;
                    else if (DsState.State.Charge < ShieldMaxCharge * 0.1) DsState.State.ShieldPercent = 0f;
                    else DsState.State.ShieldPercent = 100f;

                    ShieldChargeRate = 0f;
                    _shieldConsumptionRate = 0f;
                    return true;
                }
            }

            if (serverNoPower)
            {
                _powerNoticeLoop++;
                if (_powerNoticeLoop >= PowerNoticeCount)
                {
                    DsState.State.NoPower = false;
                    _powerNoticeLoop = 0;
                    if (Session.Enforced.Debug >= 3) Log.Line($"StateUpdate: PowerRestored - ShieldId [{Shield.EntityId}]");
                    ShieldChangeState();
                }
            }
            return false;
        }
        #endregion
    }
}