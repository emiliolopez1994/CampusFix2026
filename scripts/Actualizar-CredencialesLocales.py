"""One-off local credential migration; connection supplied through PG* environment variables."""
import base64, hashlib, json, os, re, struct, subprocess, unicodedata, uuid
from datetime import datetime
from pathlib import Path

PRIVATE = Path(os.environ['LOCALAPPDATA']) / 'CampusFix/integral-2026-10-10'
PG = Path(r'C:\Program Files\PostgreSQL\18\bin')
BATCH = 'institutional-2026-10-10-v1'
assert os.environ.get('PGHOST') == 'localhost' and os.environ.get('PGDATABASE') == 'campusfix'

def sql(statement):
    result = subprocess.run([str(PG/'psql.exe'), '-X', '-qAt', '-v', 'ON_ERROR_STOP=1'], input=statement, text=True, encoding='utf-8', capture_output=True)
    if result.returncode:
        raise RuntimeError('PostgreSQL rechazó la operación; no se muestran datos sensibles.')
    return result.stdout.strip()

def literal(value):
    return "'" + value.replace("'", "''") + "'"

SNAPSHOT = '''SELECT json_build_object(
 'users',(SELECT json_agg(row_to_json(u) ORDER BY "Id") FROM "AspNetUsers" u),
 'documents',(SELECT md5(string_agg(row_to_json(d)::text,'' ORDER BY "Kind","Id")) FROM "Documentos" d WHERE "Kind"<>'sessions'),
 'roles',(SELECT md5(string_agg(row_to_json(r)::text,'' ORDER BY "UserId","RoleId")) FROM "AspNetUserRoles" r))'''
before = json.loads(sql(SNAPSHOT))
assert sql('SELECT host(inet_server_addr())') in ('127.0.0.1','::1')
users = before['users']
def profile(user):
    return json.loads(user['Profile']) if isinstance(user['Profile'], str) else user['Profile']
targets = sorted([u for u in users if profile(u).get('syntheticOrigin') == BATCH], key=lambda u:u['Id'])
originals = [u for u in users if u not in targets]
assert len(users)==62 and len(targets)==52 and len(originals)==10
access_path = PRIVATE/'accesos-locales.json'
access = json.loads(access_path.read_text(encoding='utf-8-sig'))
assert {c['id'] for c in access} == {u['Id'] for u in targets}
used = {u['Email'].lower() for u in originals}
updated = []
for user in targets:
    p = profile(user)
    words = ''.join(c for c in unicodedata.normalize('NFD',p['nombre']) if unicodedata.category(c)!='Mn').lower().split()
    assert len(words)>=2 and re.fullmatch('[a-z]+',words[0]) and re.fullmatch('[a-z]+',words[1])
    stem=words[0][0]+words[1]
    email=stem+'@campusfix.com'
    suffix=1
    while email in used:
        email=f'{stem}{suffix}@campusfix.com'; suffix+=1
    used.add(email)
    # Explicitly approved exception for the seven "Administración de …" accounts.
    password=words[0][0].upper()+words[1]+('00' if words[1]=='de' else '')+'123456#'
    assert 12<=len(password)<=128
    salt=os.urandom(16)
    # ASP.NET Core Identity V3: marker, SHA512 PRF, iterations, salt length, salt, subkey.
    digest=base64.b64encode(b'\x01'+struct.pack('>III',2,100000,len(salt))+salt+hashlib.pbkdf2_hmac('sha512',password.encode(),salt,100000,32)).decode()
    updated.append(dict(id=user['Id'],correo=email,password=password,passwordHash=digest,nombre=p['nombre'],role=p['role'],areaId=p.get('areaId','')))
stamp=datetime.now().strftime('%Y%m%d-%H%M%S')
backup=PRIVATE/f'antes-credenciales-{stamp}.dump'
subprocess.run([str(PG/'pg_dump.exe'),'-Fc','-f',str(backup)],check=True,capture_output=True)
assert backup.stat().st_size>0
(PRIVATE/f'accesos-antes-credenciales-{stamp}.json').write_bytes(access_path.read_bytes())
# Preserve a recovery manifest before committing; never print passwords or hashes.
(PRIVATE/f'credenciales-manifiesto-{stamp}.json').write_text(json.dumps(updated,ensure_ascii=False,indent=2),encoding='utf-8')
statements=['BEGIN;', 'LOCK TABLE "AspNetUsers" IN EXCLUSIVE MODE;']
for item in updated:
    old=next(u for u in targets if u['Id']==item['id'])
    statements.append('DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM "AspNetUsers" WHERE "Id"='+literal(item['id'])+' AND "ConcurrencyStamp" IS NOT DISTINCT FROM '+literal(old['ConcurrencyStamp'] or '')+') THEN RAISE EXCEPTION \'La cuenta cambió; repetir la previsualización.\'; END IF; END $$;')
    fields={'Email':item['correo'],'UserName':item['correo'],'NormalizedEmail':item['correo'].upper(),'NormalizedUserName':item['correo'].upper(),'PasswordHash':item['passwordHash'],'SecurityStamp':str(uuid.uuid4()),'ConcurrencyStamp':str(uuid.uuid4())}
    statements.append('UPDATE "AspNetUsers" SET '+','.join('"'+k+'"='+literal(v) for k,v in fields.items())+',"Version"="Version"+1 WHERE "Id"='+literal(item['id'])+';')
statements.append('COMMIT;')
sql('\n'.join(statements))
after=json.loads(sql(SNAPSHOT))
assert after['documents']==before['documents'] and after['roles']==before['roles']
by_id={u['Id']:u for u in after['users']}
assert len(by_id)==62 and len({u['Email'].lower() for u in by_id.values()})==62
for user in originals:
    assert user==by_id[user['Id']]
mutable={'Email','UserName','NormalizedEmail','NormalizedUserName','PasswordHash','SecurityStamp','ConcurrencyStamp','Version'}
for user in targets:
    assert {k:v for k,v in user.items() if k not in mutable}=={k:v for k,v in by_id[user['Id']].items() if k not in mutable}
for credential in access:
    item=next(i for i in updated if i['id']==credential['id'])
    credential.update(correo=item['correo'],password=item['password'],policy='Formato académico fijo; excepción de 12 caracteres autorizada para Administración de.')
access_path.write_text(json.dumps(access,ensure_ascii=False,indent=2),encoding='utf-8')
areas=json.loads(sql('SELECT json_object_agg("Id","Data"->>\'nombre\') FROM "Documentos" WHERE "Kind"=\'config\' AND "Data"->>\'kind\'=\'areas\''))
lines=['# Accesos locales actualizados','','Aplicación: http://localhost:4200. Solo PostgreSQL local. Los IDs internos permanecen intactos.','','## 52 cuentas sintéticas','','| Rol | Área | Nombre completo | Correo electrónico | Contraseña |','|---|---|---|---|---|']
for i in sorted(updated,key=lambda i:(i['role'],areas.get(i['areaId'],''),i['nombre'],i['correo'])):
    lines.append('| '+' | '.join([i['role'],areas.get(i['areaId'],'Sin área'),i['nombre'],i['correo'],i['password']])+' |')
lines+=['','## Diez cuentas originales — sin modificaciones','','| Rol | Área | Nombre completo | Correo electrónico | Contraseña |','|---|---|---|---|---|']
for u in sorted(originals,key=lambda u:(profile(u)['role'],u['Email'])):
    p=profile(u);lines.append('| '+' | '.join([p['role'],areas.get(p.get('areaId',''),'Sin área'),p['nombre'],u['Email'],'Conservada; usar la actual'])+' |')
output=PRIVATE/'ACCESOS_LOCALES_ACTUALIZADOS.md'
output.write_text('\n'.join(lines)+'\n',encoding='utf-8')
(PRIVATE/'verificacion-credenciales.json').write_text(json.dumps({'users':62,'updated':52,'originalsUnchanged':10,'duplicates':0,'documentsDigest':after['documents'],'rolesDigest':after['roles'],'backup':str(backup),'accessFile':str(output)},indent=2),encoding='utf-8')
print('52 credenciales actualizadas; 62 usuarios; cero duplicados; diez originales y documentos intactos.')
