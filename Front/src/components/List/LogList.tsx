import React from 'react'
import { useQuery } from '@apollo/client'
import {
  Typography,
  CardContent,
  CircularProgress,
  TableContainer,
  Table,
  TableBody,
  TableHead,
  TableCell,
  TableRow,
  TableFooter,
  TablePagination
} from '@material-ui/core';
import {
  StyledTableCell,
  StyledTableRow,
  TablePaginationActions,
  convUTC2JST
} from '../Common'
import { Log, Logs, OffsetPaginationVars, LOG_QUERY } from '../Common/types'

const LogList: React.FC<any> = (props: any) => {
  const [page, setPage] = React.useState(0);
  const [rowsPerPage, setRowsPerPage] = React.useState(10);
  const { loading, error, data } = useQuery<Logs, OffsetPaginationVars>(LOG_QUERY, {
    variables: { skip: page * rowsPerPage, take: rowsPerPage }
  })
  if (error) return <p>Got Error...</p>

  const handleChangePage = (event: React.MouseEvent<HTMLButtonElement> | null, newPage: number) => {
    setPage(newPage)
  };

  const handleChangeRowsPerPage = (
    event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>,
  ) => {
    setRowsPerPage(parseInt(event.target.value, 10))
    setPage(0)
  };

  return (
    <>
      {loading ? <CircularProgress /> :
        <TableContainer component={CardContent}>
          <Typography variant="h6">Counter Logs</Typography>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Id</TableCell>
                <TableCell>NodeId</TableCell>
                <TableCell>Count</TableCell>
                <TableCell>RecordTime</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data?.logs.items.map((log: Log) =>
                <StyledTableRow key={log.id}>
                  <StyledTableCell>{log.id}</StyledTableCell>
                  <StyledTableCell component="th" scope="row">{log.nodeId}</StyledTableCell>
                  <StyledTableCell>{log.count}</StyledTableCell>
                  <StyledTableCell>{convUTC2JST(log.recordTime)}</StyledTableCell>
                </StyledTableRow>
              )}
            </TableBody>
            <TableFooter>
              <TableRow>
                <TablePagination
                  rowsPerPageOptions={[5, 10, 25]}
                  count={data ? data.logs.totalCount : 0}
                  rowsPerPage={rowsPerPage}
                  page={page}
                  SelectProps={{
                    inputProps: { 'aria-label': 'rows per page' },
                    native: true,
                  }}
                  onChangePage={handleChangePage}
                  onChangeRowsPerPage={handleChangeRowsPerPage}
                  ActionsComponent={TablePaginationActions} />
              </TableRow>
            </TableFooter>
          </Table>
        </TableContainer>
      }
    </>
  );
}

export { LogList }