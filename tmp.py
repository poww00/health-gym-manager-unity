import sys

file_path = r'D:\unity\My project\Assets\_Project\Scripts\Runtime\Core\CustomerFlowManager.cs'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

target = '''                    break;
                }
                    if (customer.retrySearchCountdown <= 0f)'''

replacement = '''                    break;
                }

                case CustomerState.UsingMachine:
                {
                    customer.remainingUseSeconds -= simulationDeltaTime;
                    UpdateCustomerVisualAnimation(customer);

                    if (customer.remainingUseSeconds <= 0f)
                    {
                        if (machines != null)
                        {
                            var machine = machines.FirstOrDefault(m => m.key == customer.targetMachineKey);
                            if (machine != null && machine.data != null && machine.data.runtimeDefinition != null)
                            {
                                float breakdownChance = EquipmentBrandTierRules.GetBreakdownChancePerUse(machine.data.runtimeDefinition.BrandTier);
                                if (UnityEngine.Random.value < breakdownChance)
                                {
                                    machine.data.isBroken = true;
                                    Debug.Log($"[CustomerFlowManager] 기구 고장 발생! ({machine.data.runtimeDefinition.DisplayName})");
                                    PushOperationFeed("기구가 고장났습니다! 수리가 필요해", operationFeedAlertColor);
                                }
                            }
                        }

                        ReleaseReservationIfNeeded(customer);
                        customer.remainingMachineStops -= 1;

                        if (customer.remainingMachineStops > 0 && machines != null && machines.Count > 0)
                        {
                            if (!TryAssignNextMachine(customer, machines))
                            {
                                EnterWaitingState(customer);
                            }
                        }
                        else
                        {
                            BeginLeaving(customer, CustomerLeaveReason.CompletedVisit);
                        }
                    }

                    break;
                }

                case CustomerState.WaitingForMachine:
                {
                    customer.remainingWaitSeconds -= simulationDeltaTime;
                    customer.retrySearchCountdown -= simulationDeltaTime;
                    dailyTotalWaitSeconds += simulationDeltaTime;
                    UpdateCustomerVisualAnimation(customer);

                    // Move to waiting slot visual position
                    if (customer.waitSlotIndex >= 0)
                    {
                        Vector3 slotPos = GetWaitingWorldPosition(customer.waitSlotIndex);
                        UpdateCustomerFacingDirection(customer, slotPos);
                        customer.worldPosition = Vector3.MoveTowards(customer.worldPosition, slotPos, moveStep);
                        if (customer.visual != null)
                        {
                            customer.visual.transform.position = customer.worldPosition;
                        }
                    }

                    if (customer.retrySearchCountdown <= 0f)'''

if target in content:
    new_content = content.replace(target, replacement)
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(new_content)
    print('Successfully patched CustomerFlowManager.cs')
else:
    print('Target not found in file!')
