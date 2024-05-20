# Cron tab for root user:
# 1 */12 * * * /bin/bash {PATH_TO_BACKUP_GENERATOR_SCRIPT}

container_name="base_mssql_1"
username="sa"
password="PASSWORD"
backupdir="/var/opt/mssql/backups"
DATE=$(date +%Y%m%d-%H%M%S) # YYYYMMDD-HHMMSS

DATABASES=$(docker exec $container_name /opt/mssql-tools/bin/sqlcmd -S "$container_name" -U "$username" -P "$password" -Q "SELECT Name from sys.Databases" | grep -Ev "(-|Name|master|tempdb|model|msdb|affected\)$|\s\n|^$)")
# Iterate over all of our databases and back them up one by one...
for DBNAME in $DATABASES; do
        echo -n "Dumping database : $DBNAME... "
        docker exec $container_name /opt/mssql-tools/bin/sqlcmd -S "$container_name" -U "$username" -P "$password" -Q "BACKUP DATABASE [${DBNAME}] TO  DISK = '${backupdir}/${DBNAME}_${DATE}.bak' WITH NOFORMAT, NOINIT, NAME = '${DBNAME}-full', SKIP,NOREWIND, NOUNLOAD, STATS = 10"
        echo "Done"
done
