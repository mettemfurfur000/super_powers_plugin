void CS2Fixes::Hook_CheckTransmit(CCheckTransmitInfo **ppInfoList, int infoCount, CBitVec<16384> &unionTransmitEdicts,
                                  const Entity2Networkable_t **pNetworkables, const uint16 *pEntityIndicies, int nEntities, bool bEnablePVSBits)
{
    if (!g_pEntitySystem)
        return;

    VPROF("CS2Fixes::Hook_CheckTransmit");

    for (int i = 0; i < infoCount; i++)
    {
        auto &pInfo = ppInfoList[i];

        // the offset happens to have a player index here,
        // though this is probably part of the client class that contains the CCheckTransmitInfo
        static int offset = g_GameConfig->GetOffset("CheckTransmitPlayerSlot");
        int iPlayerSlot = (int)*((uint8 *)pInfo + offset);

        CCSPlayerController *pSelfController = CCSPlayerController::FromSlot(iPlayerSlot);

        if (!pSelfController || !pSelfController->IsConnected())
            continue;

        auto pSelfZEPlayer = g_playerManager->GetPlayer(iPlayerSlot);

        if (!pSelfZEPlayer)
            continue;

        for (int j = 0; j < gpGlobals->maxClients; j++)
        {
            CCSPlayerController *pController = CCSPlayerController::FromSlot(j);

            // Always transmit to themselves
            if (!pController || j == iPlayerSlot)
                continue;

            // Don't transmit other players' flashlights, except the one they're watching if in spec
            CBarnLight *pFlashLight = pController->IsConnected() ? g_playerManager->GetPlayer(j)->GetFlashLight() : nullptr;

            if (!g_bFlashLightTransmitOthers && pFlashLight &&
                !(pSelfController->GetPawnState() == STATE_OBSERVER_MODE && pSelfController->GetObserverTarget() == pController->GetPawn()))
            {
                pInfo->m_pTransmitEntity->Clear(pFlashLight->entindex());
            }

            // Always transmit other players if spectating
            if (!g_bEnableHide || pSelfController->GetPawnState() == STATE_OBSERVER_MODE)
                continue;

            // Get the actual pawn as the player could be currently spectating
            CCSPlayerPawn *pPawn = pController->GetPlayerPawn();

            if (!pPawn)
                continue;

            // Hide players marked as hidden or ANY dead player, it seems that a ragdoll of a previously hidden player can crash?
            // TODO: Revert this if/when valve fixes the issue?
            // Also do not hide leaders to other players
            ZEPlayer *pOtherZEPlayer = g_playerManager->GetPlayer(j);
            if ((pSelfZEPlayer->ShouldBlockTransmit(j) && (pOtherZEPlayer && !pOtherZEPlayer->IsLeader())) || !pPawn->IsAlive())
                pInfo->m_pTransmitEntity->Clear(pPawn->entindex());
        }

        // Don't transmit glow model to it's owner
        CBaseModelEntity *pGlowModel = pSelfZEPlayer->GetGlowModel();

        if (pGlowModel)
            pInfo->m_pTransmitEntity->Clear(pGlowModel->entindex());
    }
}