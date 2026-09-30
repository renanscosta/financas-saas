-- Read-only diagnostics. Review every orphan before running VALIDATE CONSTRAINT.
SELECT 'Tenants -> Customers' AS relation, COUNT(*) AS orphan_count
FROM "Tenants" tenant
LEFT JOIN "Customers" customer ON customer."Id" = tenant."CustomerId"
WHERE customer."Id" IS NULL;

SELECT 'Provisionings -> Tenants' AS relation, COUNT(*) AS orphan_count
FROM "Provisionings" provisioning
LEFT JOIN "Tenants" tenant ON tenant."Id" = provisioning."TenantId"
WHERE tenant."Id" IS NULL;

SELECT 'Users -> Tenants' AS relation, COUNT(*) AS orphan_count
FROM "AspNetUsers" app_user
LEFT JOIN "Tenants" tenant ON tenant."Id" = app_user."TenantId"
WHERE app_user."TenantId" IS NOT NULL AND tenant."Id" IS NULL;

SELECT 'Invitations -> Customers' AS relation, COUNT(*) AS orphan_count
FROM "Invitations" invitation
LEFT JOIN "Customers" customer ON customer."Id" = invitation."CustomerId"
WHERE customer."Id" IS NULL;

SELECT 'Invitations -> Tenants' AS relation, COUNT(*) AS orphan_count
FROM "Invitations" invitation
LEFT JOIN "Tenants" tenant ON tenant."Id" = invitation."TenantId"
WHERE tenant."Id" IS NULL;

SELECT 'Invitations -> Users' AS relation, COUNT(*) AS orphan_count
FROM "Invitations" invitation
LEFT JOIN "AspNetUsers" app_user ON app_user."Id" = invitation."UserId"
WHERE app_user."Id" IS NULL;

SELECT 'AuditEntries -> Tenants' AS relation, COUNT(*) AS orphan_count
FROM "AuditEntries" audit
LEFT JOIN "Tenants" tenant ON tenant."Id" = audit."TenantId"
WHERE audit."TenantId" IS NOT NULL AND tenant."Id" IS NULL;

-- Run only after every orphan_count above is zero and remediation is approved.
ALTER TABLE "AspNetUsers" VALIDATE CONSTRAINT "FK_AspNetUsers_Tenants_TenantId";
ALTER TABLE "AuditEntries" VALIDATE CONSTRAINT "FK_AuditEntries_Tenants_TenantId";
ALTER TABLE "Invitations" VALIDATE CONSTRAINT "FK_Invitations_AspNetUsers_UserId";
ALTER TABLE "Invitations" VALIDATE CONSTRAINT "FK_Invitations_Customers_CustomerId";
ALTER TABLE "Invitations" VALIDATE CONSTRAINT "FK_Invitations_Tenants_TenantId";
ALTER TABLE "Provisionings" VALIDATE CONSTRAINT "FK_Provisionings_Tenants_TenantId";
ALTER TABLE "Tenants" VALIDATE CONSTRAINT "FK_Tenants_Customers_CustomerId";