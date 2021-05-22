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
  TablePagination,
  Button
} from '@material-ui/core';
import { Link } from 'react-router-dom'
import {
  StyledTableCell,
  StyledTableRow,
  TablePaginationActions,
  convUTC2JST
} from '../Common'
import { Counter, Latests, OffsetPaginationVars, LATEST_QUERY, COUNT_SUBSCRIPTION } from '../Common/types'

const Count: React.FC<any> = (props: any) => {
  const [page, setPage] = React.useState(0);
  const [rowsPerPage, setRowsPerPage] = React.useState(5);
  const { loading, error, data, subscribeToMore } = useQuery<Latests, OffsetPaginationVars>(LATEST_QUERY, {
    variables: { skip: page * rowsPerPage, take: rowsPerPage }
  })

  React.useEffect(() => subscribeToMore({ document: COUNT_SUBSCRIPTION }), [subscribeToMore])

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
          <Typography variant="h6">Latest Counter</Typography>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>NodeId</TableCell>
                <TableCell>Count</TableCell>
                <TableCell>RecordTime</TableCell>
                <TableCell align="right">&nbsp;</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data?.latests.items.map((counter: Counter) =>
                <StyledTableRow key={counter.nodeId}>
                  <StyledTableCell component="th" scope="row">{counter.nodeId}</StyledTableCell>
                  <StyledTableCell>{counter.count}</StyledTableCell>
                  <StyledTableCell>{convUTC2JST(counter.recordTime)}</StyledTableCell>
                  <StyledTableCell align="right">
                    <Button variant="contained" component={Link} to={`detail/${counter.nodeId}`}>Detail</Button>
                  </StyledTableCell>
                </StyledTableRow>
              )}
            </TableBody>
            <TableFooter>
              <TableRow>
                <TablePagination
                  rowsPerPageOptions={[5, 10, 25]}
                  count={data ? data.latests.totalCount : 0}
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

export { Count }