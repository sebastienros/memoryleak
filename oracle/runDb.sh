docker run --name myOracle1930 \
 -p 1521:1521 \
 -p 5500:5500 \
 -e ORACLE_SID=ORCLCDB \
 -e ORACLE_PDB=ORCLPDB1 \
 -e ORACLE_PWD=root \
 -e INIT_SGA_SIZE=1024 \
 -e INIT_PGA_SIZE=1024 \
 -e ORACLE_CHARACTERSET=AL32UTF8 \
 oracle/database:19.3.0-ee

#  sqlplus sys/root@localhost:1521/ORCLPDB1 as sysdba
#  sqlplus sys/root@//localhost:1521/ORCLCDB as sysdba
#  sqlplus system/root@//localhost:1521/ORCLCDB
#  sqlplus pdbadmin/root@//localhost:1521/ORCLCDB

# install oracle instant client
# https://gist.github.com/bmaupin/1d376476a2b6548889b4dd95663ede58


# CREATE USER foo IDENTIFIED BY abcd1234;
# GRANT CREATE SESSION TO foo;

CREATE TABLE foo.T_TOAST
( C_ID varchar2(50) NOT NULL,
C_MAT varchar2(50) NOT NULL,
C_MAT_DESC varchar2(50),
PRIMARY KEY(C_ID)
);

INSERT INTO foo.T_TOAST(C_ID, C_MAT, C_MAT_DESC) VALUES('1', 'mat-1', 'desc-1');
INSERT INTO foo.T_TOAST(C_ID, C_MAT, C_MAT_DESC) VALUES('2', 'mat-2', 'desc-2');
INSERT INTO foo.T_TOAST(C_ID, C_MAT, C_MAT_DESC) VALUES('3', 'mat-3', 'desc-3');
INSERT INTO foo.T_TOAST(C_ID, C_MAT, C_MAT_DESC) VALUES('4', 'mat-4', 'desc-4');

# SELECT * FROM T_TOAST;