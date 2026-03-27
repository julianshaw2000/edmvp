-- ============================================================
-- auditraks Demo Seed Data
-- Run against the Neon database to create demo users.
-- After running this SQL, register each user via the API.
-- ============================================================

DO $$
DECLARE
    v_tenant_id uuid;
    v_buyer_id uuid;
    v_admin_id uuid;
BEGIN

SELECT "Id" INTO v_tenant_id FROM tenants WHERE "Name" = 'Pilot Tenant' LIMIT 1;

IF v_tenant_id IS NULL THEN
    RAISE NOTICE 'No Pilot Tenant found. Skipping.';
    RETURN;
END IF;

-- Create buyer user (pending registration)
IF NOT EXISTS (SELECT 1 FROM users WHERE "Email" = 'buyer@auditraks.com') THEN
    v_buyer_id := gen_random_uuid();
    INSERT INTO users ("Id", identity_user_id, "Email", "DisplayName", "Role", "TenantId", "IsActive", "CreatedAt", "UpdatedAt")
    VALUES (v_buyer_id, 'pending|' || v_buyer_id::text, 'buyer@auditraks.com', 'Klaus Steinberger', 'BUYER', v_tenant_id, true, now(), now());
    RAISE NOTICE 'Created buyer user: buyer@auditraks.com';
ELSE
    -- Ensure it has pending| prefix so register works
    UPDATE users SET identity_user_id = 'pending|' || "Id"::text
    WHERE "Email" = 'buyer@auditraks.com' AND identity_user_id NOT LIKE 'pending|%';
    RAISE NOTICE 'Buyer user already exists';
END IF;

-- Create tenant admin user (pending registration)
IF NOT EXISTS (SELECT 1 FROM users WHERE "Email" = 'admin@auditraks.com') THEN
    v_admin_id := gen_random_uuid();
    INSERT INTO users ("Id", identity_user_id, "Email", "DisplayName", "Role", "TenantId", "IsActive", "CreatedAt", "UpdatedAt")
    VALUES (v_admin_id, 'pending|' || v_admin_id::text, 'admin@auditraks.com', 'Marie Uwimana', 'TENANT_ADMIN', v_tenant_id, true, now(), now());
    RAISE NOTICE 'Created admin user: admin@auditraks.com';
ELSE
    UPDATE users SET identity_user_id = 'pending|' || "Id"::text
    WHERE "Email" = 'admin@auditraks.com' AND identity_user_id NOT LIKE 'pending|%';
    RAISE NOTICE 'Admin user already exists';
END IF;

-- Add notifications
INSERT INTO notifications ("Id", "TenantId", "UserId", "Type", "Title", "Message", "ReferenceId", "IsRead", "EmailSent", "EmailRetryCount", "CreatedAt")
SELECT gen_random_uuid(), v_tenant_id, u."Id", 'COMPLIANCE', 'Batch W-2026-038 flagged',
    'OECD DDG check flagged batch W-2026-038 due to DRC origin (high-risk CAHRA). Enhanced due diligence required.',
    NULL, false, false, 0, now() - interval '2 days'
FROM users u WHERE u."Email" = 'supplier@auditraks.com' AND u."TenantId" = v_tenant_id
AND NOT EXISTS (SELECT 1 FROM notifications n WHERE n."UserId" = u."Id" AND n."Title" = 'Batch W-2026-038 flagged');

INSERT INTO notifications ("Id", "TenantId", "UserId", "Type", "Title", "Message", "ReferenceId", "IsRead", "EmailSent", "EmailRetryCount", "CreatedAt")
SELECT gen_random_uuid(), v_tenant_id, u."Id", 'BATCH_STATUS', 'Batch W-2026-041 completed',
    'All custody events recorded and compliance checks passed. Material Passport ready for generation.',
    NULL, true, true, 0, now() - interval '5 days'
FROM users u WHERE u."Email" = 'supplier@auditraks.com' AND u."TenantId" = v_tenant_id
AND NOT EXISTS (SELECT 1 FROM notifications n WHERE n."UserId" = u."Id" AND n."Title" = 'Batch W-2026-041 completed');

RAISE NOTICE '';
RAISE NOTICE '=== Demo users ready ===';
RAISE NOTICE 'Register each via: POST /api/auth/register';
RAISE NOTICE '  supplier@auditraks.com (already exists, register to set password)';
RAISE NOTICE '  buyer@auditraks.com';
RAISE NOTICE '  admin@auditraks.com';
RAISE NOTICE '';
RAISE NOTICE 'Existing batches (from app seed):';
RAISE NOTICE '  W-2026-041 — COMPLIANT, full journey (Rwanda, 6 events)';
RAISE NOTICE '  W-2026-038 — FLAGGED, DRC high-risk (4 events)';
RAISE NOTICE '  W-2026-045 — PENDING, no events yet (Bolivia)';
RAISE NOTICE '  W-2026-035 — COMPLIANT, cassiterite (Rwanda, 5 events)';

END $$;
